using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using AppCheck.Data;
using AppCheck.Data.Entities;
using AppCheck.API.Services;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChecadaController : ControllerBase
{
    private readonly AppCheckDbContext _db;
    private readonly FacialRecognitionService _facialService;

    public ChecadaController(AppCheckDbContext db, FacialRecognitionService facialService)
    {
        _db = db;
        _facialService = facialService;
    }

    public class ChecadaRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string TokenPuntoLectura { get; set; } = string.Empty;
        public string TipoChecada { get; set; } = "Entrada";
        public string MetodoUsado { get; set; } = "QR"; // QR, PIN, QR+Rostro, PIN+Rostro, Rostro
        public string? FotoBase64 { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
        public DateTimeOffset? TimestampLocal { get; set; }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarChecada([FromBody] ChecadaRequest request)
    {
        // 1. Validar Punto de Lectura
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == request.TokenPuntoLectura && p.EstadoLicencia != "Suspendido");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Punto de lectura no autorizado o suspendido." });

        // 2. Buscar empleado
        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado && e.CuentaId == puntoLectura.CuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = $"Empleado {request.NumeroEmpleado} no encontrado en esta empresa." });

        if (empleado.Estado != "Activo")
            return BadRequest(new { mensaje = "El empleado está inactivo. Contacta al administrador." });

        // 3. Validación facial con IA REAL (para métodos "Rostro", "QR+Rostro" y "PIN+Rostro")
        if (request.MetodoUsado == "Rostro" || request.MetodoUsado == "QR+Rostro" || request.MetodoUsado == "PIN+Rostro")
        {
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto para la validación facial." });

            if (string.IsNullOrEmpty(empleado.FaceTemplateHash))
                return BadRequest(new { mensaje = "El empleado no tiene un rostro registrado en el sistema." });

            // Generar embedding de la foto en vivo
            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro en la foto." });

            // Convertir el embedding guardado (string) a float[]
            var embeddingGuardado = empleado.FaceTemplateHash
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(float.Parse)
                .ToArray();

            // Comparar embeddings
            var similitud = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

            // Umbral para IA real (60% es un buen punto de partida)
            if (similitud < 70)
            {
                return Unauthorized(new { mensaje = $"Rostro no coincide. Similitud: {similitud}% (mínimo 70%). Intenta de nuevo." });
            }
        }

        // 4. Determinar Entrada/Salida
        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal;
        if (ultimaChecada == null)
            tipoChecadaFinal = "Entrada";
        else if (ultimaChecada.TipoChecada == "Entrada")
            tipoChecadaFinal = "Salida";
        else
            tipoChecadaFinal = "Entrada";

        // 5. Generar hash de inmutabilidad
        var timestampServidor = DateTimeOffset.UtcNow;
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|{puntoLectura.PuntoLecturaId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // 6. Guardar registro
        var nuevoRegistro = new RegistroJornada
        {
            EmpleadoId = empleado.EmpleadoId,
            PuntoLecturaId = puntoLectura.PuntoLecturaId,
            TipoChecada = tipoChecadaFinal,
            TimestampServidor = timestampServidor,
            TimestampLocal = request.TimestampLocal ?? timestampServidor,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            MetodoUsado = request.MetodoUsado,
            HashRegistro = hash,
            EsOffline = request.EsOffline
        };

        _db.RegistrosJornada.Add(nuevoRegistro);
        puntoLectura.UltimoHeartbeat = timestampServidor;
        await _db.SaveChangesAsync();

        var saludo = tipoChecadaFinal == "Entrada" ? "¡Buen día" : "¡Hasta luego";
        var mensaje = $"{saludo}, {empleado.NombreCompleto}! Tu {tipoChecadaFinal.ToLower()} ha sido registrada.";

        return Ok(new
        {
            mensaje,
            empleado = new { empleado.EmpleadoId, empleado.NumeroEmpleado, empleado.NombreCompleto },
            tipoChecada = tipoChecadaFinal,
            timestampServidor,
            hash,
            esOffline = request.EsOffline,
            puntoLectura = new { puntoLectura.PuntoLecturaId, puntoLectura.Nombre, puntoLectura.Tipo }
        });
    }

    // GET: Historial de checadas (protegido por JWT)
    [HttpGet("historial/{empleadoId}")]
    [Authorize]
    public async Task<IActionResult> HistorialEmpleado(Guid empleadoId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.CuentaId == cuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado en tu cuenta." });

        var registros = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleadoId)
            .OrderByDescending(r => r.TimestampServidor)
            .Take(30)
            .Select(r => new
            {
                r.RegistroId,
                r.TipoChecada,
                r.TimestampServidor,
                r.TimestampLocal,
                r.MetodoUsado,
                r.HashRegistro,
                r.EsOffline
            })
            .ToListAsync();

        return Ok(new
        {
            empleado = new { empleado.EmpleadoId, empleado.NombreCompleto, empleado.NumeroEmpleado },
            totalRegistros = registros.Count,
            registros
        });
    }
}
