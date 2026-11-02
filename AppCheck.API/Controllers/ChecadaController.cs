using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using AppCheck.Data;
using AppCheck.Data.Entities;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChecadaController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public ChecadaController(AppCheckDbContext db)
    {
        _db = db;
    }

    // DTO para la checada
    public class ChecadaRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string TokenPuntoLectura { get; set; } = string.Empty;
        public string TipoChecada { get; set; } = "Entrada"; // Entrada / Salida
        public string MetodoUsado { get; set; } = "QR"; // QR / Facial
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
        public DateTimeOffset? TimestampLocal { get; set; }
    }

    [HttpPost]
    [AllowAnonymous] // El kiosko no tiene JWT de usuario, usa TokenPuntoLectura
    public async Task<IActionResult> RegistrarChecada([FromBody] ChecadaRequest request)
    {
        // 1. Validar que el Punto de Lectura exista y esté activo
        //    El token se hashea con SHA-256 antes de comparar (igual que en KioskoController)
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TokenPuntoLectura)));
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia != "Suspendido");

        // 2. Buscar el empleado en la cuenta del punto de lectura
        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado && e.CuentaId == puntoLectura.CuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = $"Empleado {request.NumeroEmpleado} no encontrado en esta empresa." });

        if (empleado.Estado != "Activo")
            return BadRequest(new { mensaje = "El empleado está inactivo. Contacta al administrador." });

        // 3. Determinar si es entrada o salida automáticamente
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == DateTimeOffset.UtcNow.Date)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal;
        if (ultimaChecada == null)
            tipoChecadaFinal = "Entrada";
        else if (ultimaChecada.TipoChecada == "Entrada")
            tipoChecadaFinal = "Salida";
        else
            tipoChecadaFinal = "Entrada"; // Si ya salió, vuelve a entrar (segundo turno)

        // 4. Generar hash de inmutabilidad
        var timestampServidor = DateTimeOffset.UtcNow;
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|{puntoLectura.PuntoLecturaId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // 5. Guardar el registro
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

        // 6. Actualizar heartbeat del punto de lectura
        puntoLectura.UltimoHeartbeat = timestampServidor;

        await _db.SaveChangesAsync();

        // 7. Mensaje de confirmación
        var saludo = tipoChecadaFinal == "Entrada" ? "¡Buen día" : "¡Hasta luego";
        var mensaje = $"{saludo}, {empleado.NombreCompleto}! Tu {tipoChecadaFinal.ToLower()} ha sido registrada.";

        return Ok(new
        {
            mensaje,
            empleado = new
            {
                empleado.EmpleadoId,
                empleado.NumeroEmpleado,
                empleado.NombreCompleto
            },
            tipoChecada = tipoChecadaFinal,
            timestampServidor,
            hash,
            esOffline = request.EsOffline,
            puntoLectura = new
            {
                puntoLectura.PuntoLecturaId,
                puntoLectura.Nombre,
                puntoLectura.Tipo
            }
        });
    }

    // GET: Historial de checadas de un empleado (protegido por JWT)
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
