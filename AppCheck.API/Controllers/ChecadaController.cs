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

    // =====================================================
    // DTOs
    // =====================================================
    public class ChecadaRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string TokenPuntoLectura { get; set; } = string.Empty;
        public string TipoChecada { get; set; } = "Entrada";
        public string MetodoUsado { get; set; } = "QR";
        public string? FotoBase64 { get; set; }
        public string? Pin { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
        public DateTimeOffset? TimestampLocal { get; set; }
        public float? LivenessScore { get; set; }
    }

    public class ChecadaEmpleadoRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string TipoChecada { get; set; } = "Entrada";
        public string MetodoUsado { get; set; } = "ID+Rostro";
        public string? FotoBase64 { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public bool EsOffline { get; set; } = false;
        public float? LivenessScore { get; set; }
    }

    // =====================================================
    // HELPER: Calcular distancia
    // =====================================================
    private static double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    // =====================================================
    // HELPER: Validar geofence
    // =====================================================
    private IActionResult? ValidarGeofence(PuntoLectura puntoLectura, decimal? latitud, decimal? longitud)
    {
        if (!puntoLectura.Latitud.HasValue || !puntoLectura.Longitud.HasValue)
            return null;

        if (!latitud.HasValue || !longitud.HasValue)
            return BadRequest(new { mensaje = "Este kiosko requiere GPS. Activa la ubicación en tu celular." });

        var distancia = CalcularDistancia(
            (double)puntoLectura.Latitud.Value,
            (double)puntoLectura.Longitud.Value,
            (double)latitud.Value,
            (double)longitud.Value
        );

        if (distancia > puntoLectura.RadioMetros)
        {
            return Unauthorized(new
            {
                mensaje = $"Estás a {distancia:F0} metros del kiosko. Máximo permitido: {puntoLectura.RadioMetros} metros."
            });
        }

        return null;
    }

    // =====================================================
    // HELPER: Validar liveness
    // =====================================================
    private IActionResult? ValidarLiveness(float? livenessScore)
    {
        if (!livenessScore.HasValue)
            return null;

        if (livenessScore.Value < 0.8f)
        {
            return Unauthorized(new
            {
                mensaje = "Liveness no verificado. Asegúrate de que eres una persona real (no una foto)."
            });
        }

        return null;
    }

    // =====================================================
    // 1. REGISTRAR CHECADA DESDE KIOSKO (APK)
    // =====================================================
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarChecada([FromBody] ChecadaRequest request)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TokenPuntoLectura)));
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia != "Suspendido");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Punto de lectura no autorizado o suspendido." });

        var geofenceError = ValidarGeofence(puntoLectura, request.Latitud, request.Longitud);
        if (geofenceError != null) return geofenceError;

        var livenessError = ValidarLiveness(request.LivenessScore);
        if (livenessError != null) return livenessError;

        Empleado? empleado = null;

        if (request.MetodoUsado == "Rostro" && string.IsNullOrEmpty(request.NumeroEmpleado))
        {
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto para el método Rostro." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro en la foto." });

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
                catch { }
            }

            if (mejorMatch == null || mejorSimilitud < 75)
            {
                return Unauthorized(new
                {
                    mensaje = $"Rostro no coincide con ningún empleado. Mejor similitud: {mejorSimilitud:F2}%."
                });
            }

            empleado = mejorMatch;
        }
        else
        {
            if (string.IsNullOrEmpty(request.NumeroEmpleado))
                return BadRequest(new { mensaje = "Se requiere el número de empleado." });

            empleado = await _db.Empleados
                .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado
                                      && e.CuentaId == puntoLectura.CuentaId);

            if (empleado == null)
                return NotFound(new { mensaje = $"Empleado {request.NumeroEmpleado} no encontrado." });
        }

        if (empleado.Estado != "Activo")
            return BadRequest(new { mensaje = "El empleado está inactivo." });

        if (request.MetodoUsado == "ID+PIN")
        {
            if (string.IsNullOrEmpty(request.Pin))
                return BadRequest(new { mensaje = "Se requiere el PIN." });

            if (empleado.PinRespaldo != request.Pin)
                return Unauthorized(new { mensaje = "PIN incorrecto." });
        }
        else if (request.MetodoUsado == "ID+Rostro" || request.MetodoUsado == "QR+Rostro")
        {
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto." });

            if (string.IsNullOrEmpty(empleado.FaceTemplateHash))
                return BadRequest(new { mensaje = "El empleado no tiene rostro registrado." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro." });

            var embeddingGuardado = empleado.FaceTemplateHash
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(float.Parse)
                .ToArray();

            var similitud = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

            if (similitud < 70)
                return Unauthorized(new { mensaje = $"Rostro no coincide. Similitud: {similitud}%." });
        }
        else if (request.MetodoUsado == "Huella")
        {
            if (!empleado.PuedeUsarHuella)
                return Unauthorized(new { mensaje = "No tienes autorización para usar huella." });
        }

        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal = ultimaChecada?.TipoChecada == "Entrada" ? "Salida" : "Entrada";

        var timestampServidor = DateTimeOffset.UtcNow;
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|{puntoLectura.PuntoLecturaId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

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

    // =====================================================
    // 2. REGISTRAR CHECADA DESDE PWA (empleado, sin kiosko)
    // =====================================================
    [HttpPost("empleado")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarChecadaEmpleado([FromBody] ChecadaEmpleadoRequest request)
    {
        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado
                                  && e.PinRespaldo == request.Pin
                                  && e.Estado == "Activo");

        if (empleado == null)
            return Unauthorized(new { mensaje = "ID o PIN incorrecto, o empleado inactivo." });

        var livenessError = ValidarLiveness(request.LivenessScore);
        if (livenessError != null) return livenessError;

        var metodosConRostro = new[] { "ID+Rostro", "QR+Rostro", "Rostro" };
        if (metodosConRostro.Contains(request.MetodoUsado))
        {
            if (string.IsNullOrEmpty(request.FotoBase64))
                return BadRequest(new { mensaje = "Se requiere la foto." });

            if (string.IsNullOrEmpty(empleado.FaceTemplateHash))
                return BadRequest(new { mensaje = "El empleado no tiene rostro registrado." });

            var embeddingEnVivo = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embeddingEnVivo.Length == 0)
                return BadRequest(new { mensaje = "No se detectó ningún rostro." });

            var embeddingGuardado = empleado.FaceTemplateHash
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(float.Parse)
                .ToArray();

            var similitud = _facialService.CompararRostros(embeddingGuardado, embeddingEnVivo);

            if (similitud < 70)
                return Unauthorized(new { mensaje = $"Rostro no coincide. Similitud: {similitud}%." });
        }

        if (request.MetodoUsado == "Huella" && !empleado.PuedeUsarHuella)
            return Unauthorized(new { mensaje = "No tienes autorización para usar huella." });

        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal = ultimaChecada?.TipoChecada == "Entrada" ? "Salida" : "Entrada";

        var timestampServidor = DateTimeOffset.UtcNow;
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|APP";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        var nuevoRegistro = new RegistroJornada
        {
            EmpleadoId = empleado.EmpleadoId,
            PuntoLecturaId = null,
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

    // =====================================================
    // 3. HISTORIAL DE CHECADAS
    // =====================================================
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
                r.EsOffline,
                r.Latitud,
                r.Longitud
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
    // 4. REPORTE DE JORNADA (STPS) con filtros
    // =====================================================
    [HttpGet("reporte")]
    [Authorize]
    public async Task<IActionResult> GenerarReporte(
        [FromQuery] DateTimeOffset fechaInicio,
        [FromQuery] DateTimeOffset fechaFin,
        [FromQuery] Guid? cuentaId,
        [FromQuery] Guid? vendedorId,
        [FromQuery] string? metodoUsado,
        [FromQuery] Guid? empleadoId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaIdUsuario = Guid.Parse(cuentaIdClaim);
        var rol = User.FindFirst("Rol")?.Value;

        var query = _db.RegistrosJornada
            .Include(r => r.Empleado)
                .ThenInclude(e => e.Cuenta)
            .Where(r => r.TimestampServidor >= fechaInicio
                     && r.TimestampServidor <= fechaFin);

        if (rol != "SuperAdmin")
        {
            query = query.Where(r => r.Empleado.CuentaId == cuentaIdUsuario);
        }
        else if (cuentaId.HasValue)
        {
            query = query.Where(r => r.Empleado.CuentaId == cuentaId.Value);
        }

        if (vendedorId.HasValue)
        {
            query = query.Where(r => r.Empleado.Cuenta.VendedorId == vendedorId.Value);
        }

        if (!string.IsNullOrEmpty(metodoUsado))
        {
            query = query.Where(r => r.MetodoUsado == metodoUsado);
        }

        if (empleadoId.HasValue)
        {
            query = query.Where(r => r.EmpleadoId == empleadoId.Value);
        }

        var registros = await query
            .OrderBy(r => r.TimestampServidor)
            .Select(r => new
            {
                r.RegistroId,
                Empleado = r.Empleado.NombreCompleto,
                NumeroEmpleado = r.Empleado.NumeroEmpleado,
                Empresa = r.Empleado.Cuenta.NombreComercial,
                CuentaId = r.Empleado.CuentaId,
                VendedorId = r.Empleado.Cuenta.VendedorId,
                r.TipoChecada,
                r.TimestampServidor,
                r.MetodoUsado,
                r.PuntoLecturaId,
                r.Latitud,
                r.Longitud,
                r.HashRegistro,
                r.EsOffline
            })
            .ToListAsync();

        return Ok(new
        {
            totalRegistros = registros.Count,
            fechaInicio,
            fechaFin,
            registros
        });
    }
}
