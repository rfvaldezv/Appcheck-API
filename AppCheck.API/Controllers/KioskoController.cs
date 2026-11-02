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
public class KioskoController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public KioskoController(AppCheckDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // 1. GENERAR CÓDIGO DE ACTIVACIÓN (protegido con JWT)
    // =====================================================
    public class GenerarCodigoRequest
    {
        public string NombreKiosko { get; set; } = string.Empty;
        public string Tipo { get; set; } = "KioskoFijo"; // KioskoFijo o Personal
    }

    [HttpPost("generar-codigo")]
    [Authorize]
    public async Task<IActionResult> GenerarCodigo([FromBody] GenerarCodigoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized(new { mensaje = "Token inválido." });

        var cuentaId = Guid.Parse(cuentaIdClaim);

        // Generar código de 8 caracteres (sin O/0, I/1 para evitar confusión)
        var alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var codigoPlano = new string(Enumerable.Range(0, 8)
            .Select(_ => alfabeto[random.Next(alfabeto.Length)]).ToArray());
        var codigoFormateado = $"{codigoPlano[..4]}-{codigoPlano[4..]}";

        // Hashear el código
        var codigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigoPlano)));

        // Guardar en BD
        var codigoActivacion = new CodigoActivacion
        {
            CuentaId = cuentaId,
            CodigoHash = codigoHash,
            Tipo = "Activacion",
            Estado = "Pendiente",
            ExpiraEn = DateTimeOffset.UtcNow.AddHours(72)
        };

        _db.CodigosActivacion.Add(codigoActivacion);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Código de activación generado. Compártelo con el dispositivo.",
            codigo = codigoFormateado,
            nombreKiosko = request.NombreKiosko,
            tipo = request.Tipo,
            expiraEn = codigoActivacion.ExpiraEn,
            instrucciones = "El dispositivo debe ingresar este código en la pantalla de activación de la app AppCheck."
        });
    }

    // =====================================================
    // 2. ACTIVAR KIOSKO (público, con código)
    // =====================================================
    public class ActivarKioskoRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string NombreKiosko { get; set; } = string.Empty;
        public string Tipo { get; set; } = "KioskoFijo";
    }

    [HttpPost("activar")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivarKiosko([FromBody] ActivarKioskoRequest request)
    {
        // 1. Normalizar el código (quitar guiones y espacios)
        var codigoPlano = request.Codigo.Replace("-", "").Replace(" ", "").ToUpper();
        var codigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigoPlano)));

        // 2. Buscar el código en la BD
        var codigoActivacion = await _db.CodigosActivacion
            .FirstOrDefaultAsync(c => c.CodigoHash == codigoHash);

        if (codigoActivacion == null)
            return NotFound(new { mensaje = "Código de activación inválido." });

        if (codigoActivacion.Estado != "Pendiente")
            return BadRequest(new { mensaje = "El código ya fue usado o está expirado." });

        if (codigoActivacion.ExpiraEn < DateTimeOffset.UtcNow)
        {
            codigoActivacion.Estado = "Expirado";
            await _db.SaveChangesAsync();
            return BadRequest(new { mensaje = "El código ha expirado." });
        }

        // 3. Verificar que el deviceId no esté ya activo en otro kiosko
        var deviceExistente = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId && p.EstadoLicencia == "Activa");

        if (deviceExistente != null)
            return BadRequest(new { mensaje = "Este dispositivo ya está registrado como kiosko activo." });

        // 4. Generar el TokenPuntoLectura (UUID + hash)
        var tokenPuntoLectura = Guid.NewGuid().ToString("N");
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPuntoLectura)));

        // 5. Crear el Punto de Lectura
        var puntoLectura = new PuntoLectura
        {
            CuentaId = codigoActivacion.CuentaId,
            Nombre = string.IsNullOrEmpty(request.NombreKiosko) ? "Kiosko Activado" : request.NombreKiosko,
            Tipo = request.Tipo,
            DeviceId = request.DeviceId,
            TokenHash = tokenHash,
            EstadoLicencia = "Activa",
            FechaExpiracionLicencia = DateTimeOffset.UtcNow.AddYears(1),
            UltimoHeartbeat = DateTimeOffset.UtcNow
        };

        _db.PuntosLectura.Add(puntoLectura);

        // 6. Marcar el código como usado
        codigoActivacion.Estado = "Usado";
        codigoActivacion.PuntoLecturaId = puntoLectura.PuntoLecturaId;

        await _db.SaveChangesAsync();

        // 7. Devolver el token en texto plano (solo esta vez)
        return Ok(new
        {
            mensaje = "Kiosko activado correctamente.",
            puntoLecturaId = puntoLectura.PuntoLecturaId,
            nombre = puntoLectura.Nombre,
            tipo = puntoLectura.Tipo,
            tokenPuntoLectura = tokenPuntoLectura,
            fechaExpiracionLicencia = puntoLectura.FechaExpiracionLicencia,
            advertencia = "Guarda este token en lugar seguro. No se volverá a mostrar."
        });
    }

    // =====================================================
    // 3. LISTAR KIOSKOS (protegido con JWT)
    // =====================================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ListarKioskos()
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var kioskos = await _db.PuntosLectura
            .Where(p => p.CuentaId == cuentaId)
            .Select(p => new
            {
                p.PuntoLecturaId,
                p.Nombre,
                p.Tipo,
                p.DeviceId,
                p.EstadoLicencia,
                p.FechaExpiracionLicencia,
                p.UltimoHeartbeat
            })
            .ToListAsync();

        return Ok(kioskos);
    }

    // =====================================================
    // 4. HEARTBEAT (público, con token)
    // =====================================================
    public class HeartbeatRequest
    {
        public string TokenPuntoLectura { get; set; } = string.Empty;
    }

    [HttpPost("heartbeat")]
    [AllowAnonymous]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.TokenPuntoLectura)));

        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia == "Activa");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Token de kiosko inválido o revocado." });

        puntoLectura.UltimoHeartbeat = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Heartbeat registrado.",
            estadoLicencia = puntoLectura.EstadoLicencia,
            fechaExpiracionLicencia = puntoLectura.FechaExpiracionLicencia
        });
    }

    // =====================================================
    // 5. REVOCAR KIOSKO (protegido con JWT)
    // =====================================================
    [HttpPost("revocar/{puntoLecturaId}")]
    [Authorize]
    public async Task<IActionResult> RevocarKiosko(Guid puntoLecturaId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.PuntoLecturaId == puntoLecturaId && p.CuentaId == cuentaId);

        if (puntoLectura == null)
            return NotFound(new { mensaje = "Kiosko no encontrado en tu cuenta." });

        puntoLectura.EstadoLicencia = "Suspendido";
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Kiosko revocado. No podrá seguir checando.", puntoLecturaId });
    }
}
