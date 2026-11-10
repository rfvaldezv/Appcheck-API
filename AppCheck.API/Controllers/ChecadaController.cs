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

    public class EditarChecadaRequest
    {
        public string? TipoChecada { get; set; }
        public DateTimeOffset? TimestampServidor { get; set; }
        public string? MetodoUsado { get; set; }
        public string? MotivoCorreccion { get; set; }
    }

    // =====================================================
    // HELPER: Calcular distancia (Haversine)
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
    // HELPER: Anti-duplicado (mismo tipo en < 5 minutos)
    // =====================================================
    private async Task<IActionResult?> ValidarAntiDuplicado(Guid empleadoId, string tipoChecada, DateTimeOffset timestampServidor)
    {
        var cincoMinutosAtras = timestampServidor.AddMinutes(-5);
        var checadaReciente = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleadoId
                     && r.TipoChecada == tipoChecada
                     && r.TimestampServidor >= cincoMinutosAtras)
            .FirstOrDefaultAsync();

        if (checadaReciente != null)
        {
            var minutos = (timestampServidor - checadaReciente.TimestampServidor).TotalMinutes;
            return BadRequest(new
            {
                mensaje = $"Ya registraste tu {tipoChecada.ToLower()} hace {minutos:F0} minutos. " +
                          $"Espera al menos 5 minutos antes de intentar de nuevo."
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
        // 1. Validar Punto de Lectura
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TokenPuntoLectura)));
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia != "Suspendido");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Punto de lectura no autorizado o suspendido." });

        // 1.5. Validar geofence
        var geofenceError = ValidarGeofence(puntoLectura, request.Latitud, request.Longitud);
        if (geofenceError != null) return geofenceError;

        // 1.6. Validar liveness
        var livenessError = ValidarLiveness(request.LivenessScore);
        if (livenessError != null) return livenessError;

        // 2. Buscar empleado
        Empleado? empleado = null;

        if (request.MetodoUsado == "Rostro" && string.IsNullOrEmpty(request.NumeroEmpleado))
        {
            // Búsqueda 1 a N
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

        // 3. Validar método
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

        // 4. Determinar Entrada/Salida
        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal = ultimaChecada?.TipoChecada == "Entrada" ? "Salida" : "Entrada";

        // 5. Anti-duplicado
        var timestampServidor = DateTimeOffset.UtcNow;
        var duplicadoError = await ValidarAntiDuplicado(empleado.EmpleadoId, tipoChecadaFinal, timestampServidor);
        if (duplicadoError != null) return duplicadoError;

        // 6. Generar hash
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|{puntoLectura.PuntoLecturaId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // 7. Guardar
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

        // Validar liveness
        var livenessError = ValidarLiveness(request.LivenessScore);
        if (livenessError != null) return livenessError;

        // Validar facial
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

        // Validar huella
        if (request.MetodoUsado == "Huella" && !empleado.PuedeUsarHuella)
            return Unauthorized(new { mensaje = "No tienes autorización para usar huella." });

        // Determinar Entrada/Salida
        var hoy = DateTimeOffset.UtcNow.Date;
        var ultimaChecada = await _db.RegistrosJornada
            .Where(r => r.EmpleadoId == empleado.EmpleadoId && r.TimestampServidor.Date == hoy)
            .OrderByDescending(r => r.TimestampServidor)
            .FirstOrDefaultAsync();

        string tipoChecadaFinal = ultimaChecada?.TipoChecada == "Entrada" ? "Salida" : "Entrada";

        // Anti-duplicado
        var timestampServidor = DateTimeOffset.UtcNow;
        var duplicadoError = await ValidarAntiDuplicado(empleado.EmpleadoId, tipoChecadaFinal, timestampServidor);
        if (duplicadoError != null) return duplicadoError;

        // Hash
        var dataToHash = $"{empleado.EmpleadoId}|{tipoChecadaFinal}|{timestampServidor.ToUnixTimeSeconds()}|APP";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // Guardar
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

    // =====================================================
    // 5. EDITAR CHECADA (solo RH - TenantAdmin o SuperAdmin)
    // =====================================================
    [HttpPut("{registroId}")]
    [Authorize]
    public async Task<IActionResult> EditarChecada(Guid registroId, [FromBody] EditarChecadaRequest request)
    {
        // Validar rol manualmente (el claim se llama "Rol", no ClaimTypes.Role)
        var rol = User.FindFirst("Rol")?.Value;
        if (rol != "SuperAdmin" && rol != "TenantAdmin")
            return Forbid();

        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);
        var usuarioId = User.FindFirst("UsuarioId")?.Value;

        var registro = await _db.RegistrosJornada
            .Include(r => r.Empleado)
            .FirstOrDefaultAsync(r => r.RegistroId == registroId);

        if (registro == null)
            return NotFound(new { mensaje = "Checada no encontrada." });

        if (rol != "SuperAdmin" && registro.Empleado.CuentaId != cuentaId)
            return Unauthorized(new { mensaje = "No tienes permiso para editar esta checada." });

        // Guardar valores originales para auditoría
        var tipoOriginal = registro.TipoChecada;
        var timestampOriginal = registro.TimestampServidor;
        var metodoOriginal = registro.MetodoUsado;

        // Aplicar cambios
        if (!string.IsNullOrEmpty(request.TipoChecada))
            registro.TipoChecada = request.TipoChecada;

        if (request.TimestampServidor.HasValue)
            registro.TimestampServidor = request.TimestampServidor.Value;

        if (!string.IsNullOrEmpty(request.MetodoUsado))
            registro.MetodoUsado = request.MetodoUsado;

        // Regenerar hash
        var dataToHash = $"{registro.EmpleadoId}|{registro.TipoChecada}|{registro.TimestampServidor.ToUnixTimeSeconds()}|{registro.PuntoLecturaId}";
        registro.HashRegistro = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

        // Guardar auditoría
        try
        {
            var log = new BitacoraAuditoria
            {
                LogId = Guid.NewGuid(),
                CuentaId = registro.Empleado.CuentaId,
                UsuarioId = usuarioId != null ? Guid.Parse(usuarioId) : null,
                Accion = "EDITAR_CHECADA",
                Detalle = $"Registro {registroId}: " +
                          $"Tipo: {tipoOriginal} → {registro.TipoChecada}, " +
                          $"Timestamp: {timestampOriginal} → {registro.TimestampServidor}, " +
                          $"Método: {metodoOriginal} → {registro.MetodoUsado}. " +
                          $"Motivo: {request.MotivoCorreccion ?? "No especificado"}",
                Fecha = DateTimeOffset.UtcNow
            };
            _db.BitacoraAuditoria.Add(log);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auditoría] No se pudo guardar el log: {ex.Message}");
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Checada editada correctamente.",
            registroId = registro.RegistroId,
            tipoChecada = registro.TipoChecada,
            timestampServidor = registro.TimestampServidor,
            metodoUsado = registro.MetodoUsado,
            hashRegistro = registro.HashRegistro
        });
    }
}
