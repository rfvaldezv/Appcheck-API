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

        // 1. Crear el Punto de Lectura en estado "Pendiente"
        var puntoLectura = new PuntoLectura
        {
            CuentaId = cuentaId,
            Nombre = request.NombreKiosko,
            Tipo = request.Tipo,
            DeviceId = null, // Se llenará cuando el dispositivo se active
            TokenHash = "", // Se llenará cuando el dispositivo se active
            EstadoLicencia = "Pendiente",
            FechaExpiracionLicencia = null,
            UltimoHeartbeat = null
        };

        _db.PuntosLectura.Add(puntoLectura);
        await _db.SaveChangesAsync();

        // 2. Generar el código de activación (8 caracteres)
        var alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var codigoPlano = new string(Enumerable.Range(0, 8)
            .Select(_ => alfabeto[random.Next(alfabeto.Length)]).ToArray());
        var codigoFormateado = $"{codigoPlano[..4]}-{codigoPlano[4..]}";

        // 3. Hashear el código
        var codigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigoPlano)));

        // 4. Guardar el código de activación vinculado al Punto de Lectura
        var codigoActivacion = new CodigoActivacion
        {
            CuentaId = cuentaId,
            PuntoLecturaId = puntoLectura.PuntoLecturaId, // 👈 Vinculado al kiosko
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
            puntoLecturaId = puntoLectura.PuntoLecturaId,
            codigo = codigoFormateado,
            nombreKiosko = puntoLectura.Nombre,
            tipo = puntoLectura.Tipo,
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
        Console.WriteLine($"=====> ActivarKiosko llamado con código: {request.Codigo}");
        
        // 1. Normalizar el código (quitar guiones y espacios)
        var codigoPlano = request.Codigo.Replace("-", "").Replace(" ", "").ToUpper();
        var codigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigoPlano)));
        
        Console.WriteLine($"=====> Código hash: {codigoHash}");

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

    // =====================================================
    // 6. EDITAR KIOSKO (protegido con JWT)
    // =====================================================
    public class EditarKioskoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    [HttpPut("{puntoLecturaId}")]
    [Authorize]
    public async Task<IActionResult> EditarKiosko(Guid puntoLecturaId, [FromBody] EditarKioskoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.PuntoLecturaId == puntoLecturaId && p.CuentaId == cuentaId);

        if (puntoLectura == null)
            return NotFound(new { mensaje = "Kiosko no encontrado en tu cuenta." });

        puntoLectura.Nombre = request.Nombre;
        puntoLectura.Tipo = request.Tipo;
        
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Kiosko actualizado correctamente.", puntoLecturaId });
    }

    // =====================================================
    // 7. ELIMINAR KIOSKO (protegido con JWT)
    // =====================================================
    [HttpDelete("{puntoLecturaId}")]
    [Authorize]
    public async Task<IActionResult> EliminarKiosko(Guid puntoLecturaId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.PuntoLecturaId == puntoLecturaId && p.CuentaId == cuentaId);

        if (puntoLectura == null)
            return NotFound(new { mensaje = "Kiosko no encontrado en tu cuenta." });

        // Verificar si tiene checadas registradas
        var tieneChecadas = await _db.RegistrosJornada
            .AnyAsync(r => r.PuntoLecturaId == puntoLecturaId);

        if (tieneChecadas)
            return BadRequest(new { mensaje = "No se puede eliminar el kiosko porque tiene registros de checadas asociados. Usa 'Revocar' en su lugar." });

        _db.PuntosLectura.Remove(puntoLectura);
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Kiosko eliminado correctamente." });
    }

    // =====================================================
    // 8. ALTA DIRECTA DE KIOSKO (protegido con JWT)
    // =====================================================
    public class AltaDirectaKioskoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "KioskoFijo";
        public string DeviceId { get; set; } = string.Empty;
    }

    [HttpPost("alta-directa")]
    [Authorize]
    public async Task<IActionResult> AltaDirectaKiosko([FromBody] AltaDirectaKioskoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        // Verificar que el deviceId no esté ya registrado
        var deviceExistente = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId && p.CuentaId == cuentaId);

        if (deviceExistente != null)
            return BadRequest(new { mensaje = "Este dispositivo ya está registrado." });

        // Generar token
        var tokenPuntoLectura = Guid.NewGuid().ToString("N");
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPuntoLectura)));

        var puntoLectura = new PuntoLectura
        {
            CuentaId = cuentaId,
            Nombre = request.Nombre,
            Tipo = request.Tipo,
            DeviceId = request.DeviceId,
            TokenHash = tokenHash,
            EstadoLicencia = "Activa",
            FechaExpiracionLicencia = DateTimeOffset.UtcNow.AddYears(1),
            UltimoHeartbeat = DateTimeOffset.UtcNow
        };

        _db.PuntosLectura.Add(puntoLectura);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Kiosko dado de alta directamente.",
            puntoLecturaId = puntoLectura.PuntoLecturaId,
            nombre = puntoLectura.Nombre,
            tipo = puntoLectura.Tipo,
            tokenPuntoLectura = tokenPuntoLectura, // ⚠️ Solo se muestra una vez
            advertencia = "Guarda este token en lugar seguro. No se volverá a mostrar."
        });
    }

    // =====================================================
    // 10. REGENERAR CÓDIGO DE ACTIVACIÓN (protegido)
    // =====================================================
    [HttpPost("{puntoLecturaId}/regenerar-codigo")]
    [Authorize]
    public async Task<IActionResult> RegenerarCodigo(Guid puntoLecturaId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized(new { mensaje = "Token inválido." });

        var cuentaId = Guid.Parse(cuentaIdClaim);

        // 1. Buscar el kiosko
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.PuntoLecturaId == puntoLecturaId && p.CuentaId == cuentaId);

        if (puntoLectura == null)
            return NotFound(new { mensaje = "Kiosko no encontrado." });

        // 2. No se puede regenerar si está activo
        if (puntoLectura.EstadoLicencia == "Activa")
            return BadRequest(new { mensaje = "El kiosko está activo. Primero revócalo para poder regenerar el código." });

        // 3. Invalidar códigos anteriores
        var codigosAnteriores = await _db.CodigosActivacion
            .Where(c => c.PuntoLecturaId == puntoLecturaId && c.Estado == "Pendiente")
            .ToListAsync();

        foreach (var codigo in codigosAnteriores)
        {
            codigo.Estado = "Expirado";
        }

        // 4. Generar nuevo código
        var alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var codigoPlano = new string(Enumerable.Range(0, 8)
            .Select(_ => alfabeto[random.Next(alfabeto.Length)]).ToArray());
        var codigoFormateado = $"{codigoPlano[..4]}-{codigoPlano[4..]}";
        var codigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigoPlano)));

        var nuevoCodigo = new CodigoActivacion
        {
            CuentaId = cuentaId,
            PuntoLecturaId = puntoLecturaId,
            CodigoHash = codigoHash,
            Tipo = "Activacion",
            Estado = "Pendiente",
            ExpiraEn = DateTimeOffset.UtcNow.AddHours(72)
        };

        _db.CodigosActivacion.Add(nuevoCodigo);

        // 5. Si estaba suspendido, volver a "Pendiente"
        if (puntoLectura.EstadoLicencia == "Suspendido")
        {
            puntoLectura.EstadoLicencia = "Pendiente";
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Código regenerado correctamente.",
            puntoLecturaId = puntoLectura.PuntoLecturaId,
            codigo = codigoFormateado,
            nombreKiosko = puntoLectura.Nombre,
            expiraEn = nuevoCodigo.ExpiraEn,
            instrucciones = "El dispositivo debe ingresar este código en la pantalla de activación."
        });
    }
}
