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
        public string MetodoUsado { get; set; } = "QR";
        public string? FotoBase64 { get; set; }
        public string? Pin { get; set; }              // ← NUEVO: para ID+PIN
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
        public DateTimeOffset? TimestampLocal { get; set; }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarChecada([FromBody] ChecadaRequest request)
    {
        // 1. Validar Punto de Lectura (hasheando el token)
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TokenPuntoLectura)));
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia != "Suspendido");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Punto de lectura no autorizado o suspendido." });

        // 2. Buscar empleado
        Empleado? empleado = null;

        if (request.MetodoUsado == "Rostro" && string.IsNullOrEmpty(request.NumeroEmpleado))
        {
            // 🔍 BÚSQUEDA 1 a N: Rostro solo (sin ID)
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto para el método Rostro." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro en la foto." });

            // Buscar en todos los empleados de la cuenta con rostro registrado
            var empleados = await _db.Empleados
                .Where(e => e.CuentaId == puntoLectura.CuentaId
                         && e.Estado == "Activo"
                         && e.FaceTemplateHash != null)
                .ToListAsync();

            Empleado? mejorMatch = null;
            double mejorSimilitud = 0;

            foreach (var emp in empleados)
            {
                try
                {
                    var embeddingGuardado = emp.FaceTemplateHash!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(float.Parse)
                        .ToArray();
                    var sim = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

                    if (sim > mejorSimilitud)
                    {
                        mejorSimilitud = sim;
                        mejorMatch = emp;
                    }
                }
                catch { /* ignorar empleado con embedding corrupto */ }
            }

            // Umbral más alto para 1-N (75% en vez de 70%)
            if (mejorMatch == null || mejorSimilitud < 75)
            {
                return Unauthorized(new 
                { 
                    mensaje = $"Rostro no coincide con ningún empleado. Mejor similitud: {mejorSimilitud:F2}% (mínimo 75% para Rostro solo)." 
                });
            }

            empleado = mejorMatch;
        }
        else
        {
            // Búsqueda normal por NumeroEmpleado
            if (string.IsNullOrEmpty(request.NumeroEmpleado))
                return BadRequest(new { mensaje = "Se requiere el número de empleado." });

            empleado = await _db.Empleados
                .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado 
                                      && e.CuentaId == puntoLectura.CuentaId);

            if (empleado == null)
                return NotFound(new { mensaje = $"Empleado {request.NumeroEmpleado} no encontrado en esta empresa." });
        }

        if (empleado.Estado != "Activo")
            return BadRequest(new { mensaje = "El empleado está inactivo. Contacta al administrador." });

        // 3. Validar método según el tipo
        if (request.MetodoUsado == "ID+PIN")
        {
            // Validar PIN
            if (string.IsNullOrEmpty(request.Pin))
                return BadRequest(new { mensaje = "Se requiere el PIN para el método ID+PIN." });

            if (empleado.PinRespaldo != request.Pin)
                return Unauthorized(new { mensaje = "PIN incorrecto." });
        }
        else if (request.MetodoUsado == "ID+Rostro" || request.MetodoUsado == "QR+Rostro")
        {
            // Validar rostro
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto para la validación facial." });

            if (string.IsNullOrEmpty(empleado.FaceTemplateHash))
                return BadRequest(new { mensaje = "El empleado no tiene un rostro registrado en el sistema." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro en la foto." });

            var embeddingGuardado = empleado.FaceTemplateHash
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(float.Parse)
                .ToArray();

            var similitud = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

            if (similitud < 70)
            {
                return Unauthorized(new { mensaje = $"Rostro no coincide. Similitud: {similitud}% (mínimo 70%)." });
            }
        }
        else if (request.MetodoUsado == "Rostro")
        {
            // Ya se validó arriba (búsqueda 1-N)
            // Nada más que hacer
        }
        // QR: sin validación adicional (el QR ya lleva el ID)

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

    // =====================================================
    // 2. CHEQUEAR COMO EMPLEADO (sin kiosko, solo ID + PIN)
    //    Para la App móvil / PWA del empleado
    // =====================================================
    public class ChecadaEmpleadoRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string TipoChecada { get; set; } = "Entrada";
        public string MetodoUsado { get; set; } = "ID+PIN";
        public string? FotoBase64 { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
    }

    [HttpPost("empleado")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarChecadaEmpleado([FromBody] ChecadaEmpleadoRequest request)
    {
        // 1. Buscar empleado por número y PIN
        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado 
                                  && e.PinRespaldo == request.Pin 
                                  && e.Estado == "Activo");

        if (empleado == null)
            return Unauthorized(new { mensaje = "ID o PIN incorrecto, o empleado inactivo." });

        // 2. Validar facial si aplica
        var metodosConRostro = new[] { "ID+Rostro", "Rostro" };
        if (metodosConRostro.Contains(request.MetodoUsado))
        {
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto para la validación facial." });

            if (string.IsNullOrEmpty(empleado.FaceTemplateHash))
                return BadRequest(new { mensaje = "El empleado no tiene un rostro registrado." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro en la foto." });

            var embeddingGuardado = empleado.FaceTemplateHash
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(float.Parse)
                .ToArray();

            var similitud = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

            if (similitud < 70)
                return Unauthorized(new { mensaje = $"Rostro no coincide. Similitud: {similitud}% (mínimo 70%)." });
        }

        // 3. Determinar Entrada/Salida
        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal = ultimaChecada?.TipoChecada == "Entrada" ? "Salida" : "Entrada";

        // 4. Generar hash
        var timestampServidor = DateTimeOffset.UtcNow;
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|APP";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // 5. Guardar registro
        var nuevoRegistro = new RegistroJornada
        {
            EmpleadoId = empleado.EmpleadoId,
            PuntoLecturaId = null, // La App del empleado no tiene kiosko asociado
            TipoChecada = tipoChecadaFinal,
            TimestampServidor = timestampServidor,
            TimestampLocal = timestampServidor,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            MetodoUsado = request.MetodoUsado,
            HashRegistro = hash,
            EsOffline = request.EsOffline
        };

        _db.RegistrosJornada.Add(nuevoRegistro);
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
            latitud = request.Latitud,
            longitud = request.Longitud
        });
    }
}
