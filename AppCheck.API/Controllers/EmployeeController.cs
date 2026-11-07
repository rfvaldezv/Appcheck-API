using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;
using AppCheck.API.Services;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly AppCheckDbContext _db;
    private readonly FacialRecognitionService _facialService;

    // 🛡️ Lista canónica de métodos válidos (5 métodos)
    private static readonly string[] MetodosValidos = new[] 
    { 
        "QR",           // QR solo (identifica)
        "QR+Rostro",    // QR + rostro (doble factor)
        "ID+PIN",       // ID + PIN (doble factor, sin cámara)
        "ID+Rostro",    // ID + rostro (doble factor, natural)
        "Rostro"        // Rostro solo (identifica por sí mismo)
    };

    public EmployeeController(AppCheckDbContext db, FacialRecognitionService facialService)
    {
        _db = db;
        _facialService = facialService;
    }

    public class AltaEmpleadoRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string PinRespaldo { get; set; } = string.Empty;
        public string MetodoLecturaPreferido { get; set; } = "QR";
        public string? FotoBase64 { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> AltaEmpleado([FromBody] AltaEmpleadoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized(new { mensaje = "Token inválido o sin cuenta asociada." });

        var cuentaId = Guid.Parse(cuentaIdClaim);

        // 🛡️ Validar que el método de lectura sea canónico
        if (!MetodosValidos.Contains(request.MetodoLecturaPreferido))
        {
            return BadRequest(new
            {
                mensaje = $"Método de lectura inválido: '{request.MetodoLecturaPreferido}'. " +
                          $"Valores permitidos: {string.Join(", ", MetodosValidos)}"
            });
        }

        // 🛡️ Validar PIN obligatorio si el método lo requiere
        if ((request.MetodoLecturaPreferido == "ID+PIN") && string.IsNullOrWhiteSpace(request.PinRespaldo))
        {
            return BadRequest(new { mensaje = "El método ID+PIN requiere un PIN." });
        }

        // 🛡️ Validar PIN único por cuenta (para evitar ambigüedad)
        if (!string.IsNullOrWhiteSpace(request.PinRespaldo))
        {
            var pinExiste = await _db.Empleados
                .AnyAsync(e => e.CuentaId == cuentaId && e.PinRespaldo == request.PinRespaldo);

            if (pinExiste)
                return BadRequest(new { mensaje = "El PIN ya está en uso por otro empleado. Elige uno diferente." });
        }

        var existe = await _db.Empleados
            .AnyAsync(e => e.CuentaId == cuentaId && e.NumeroEmpleado == request.NumeroEmpleado);

        if (existe)
            return BadRequest(new { mensaje = $"Ya existe un empleado con el número {request.NumeroEmpleado}." });

        // 👆 GENERAR EMBEDDING CON IA REAL (si se envió foto)
        string? faceEmbeddingStr = null;
        if (!string.IsNullOrEmpty(request.FotoBase64))
        {
            var embedding = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embedding.Length > 0)
            {
                faceEmbeddingStr = string.Join(",", embedding);
            }
            else
            {
                return BadRequest(new { mensaje = "No se pudo detectar el rostro en la foto enviada. Intenta con una foto de frente y bien iluminada." });
            }
        }

        // 🛡️ Validar que si el método requiere rostro, se haya enviado foto
        var metodosConRostro = new[] { "QR+Rostro", "ID+Rostro", "Rostro" };
        if (metodosConRostro.Contains(request.MetodoLecturaPreferido) && string.IsNullOrEmpty(faceEmbeddingStr))
        {
            return BadRequest(new { mensaje = $"El método {request.MetodoLecturaPreferido} requiere una foto para registrar el rostro." });
        }

        var nuevoEmpleado = new Empleado
        {
            CuentaId = cuentaId,
            NumeroEmpleado = request.NumeroEmpleado,
            NombreCompleto = request.NombreCompleto,
            PinRespaldo = request.PinRespaldo,
            MetodoLecturaPreferido = request.MetodoLecturaPreferido,
            FaceTemplateHash = faceEmbeddingStr,
            Estado = "Activo"
        };

        _db.Empleados.Add(nuevoEmpleado);
        await _db.SaveChangesAsync();

        var acuse = $@"
ACUSE DE ACUERDO DE REGISTRO ELECTRÓNICO DE JORNADA
---------------------------------------------------
Empleado: {nuevoEmpleado.NombreCompleto}
Número de Empleado: {nuevoEmpleado.NumeroEmpleado}
Método de lectura preferido: {nuevoEmpleado.MetodoLecturaPreferido}
Fecha de alta: {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm:ss} UTC

El empleado acepta que su jornada laboral será registrada mediante el sistema AppCheck,
conforme al artículo 132, fracción XXXIV de la Ley Federal del Trabajo.
";

        var acuerdo = new AcuerdoFirmado
        {
            EmpleadoId = nuevoEmpleado.EmpleadoId,
            DocumentoUrl = $"acuerdo_{nuevoEmpleado.EmpleadoId}.txt",
            FechaFirma = DateTimeOffset.UtcNow
        };
        _db.AcuerdosFirmados.Add(acuerdo);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Empleado dado de alta correctamente.",
            empleadoId = nuevoEmpleado.EmpleadoId,
            numeroEmpleado = nuevoEmpleado.NumeroEmpleado,
            nombre = nuevoEmpleado.NombreCompleto,
            metodoLectura = nuevoEmpleado.MetodoLecturaPreferido,
            tieneRostro = !string.IsNullOrEmpty(nuevoEmpleado.FaceTemplateHash),
            acuseContenido = acuse,
            acuerdoId = acuerdo.AcuerdoId
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListarEmpleados()
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var rol = User.FindFirst("Rol")?.Value;

        IQueryable<Empleado> query = _db.Empleados.Include(e => e.Cuenta);

        if (rol != "SuperAdmin")
        {
            query = query.Where(e => e.CuentaId == cuentaId);
        }

        var empleados = await query
            .Select(e => new
            {
                e.EmpleadoId,
                e.NumeroEmpleado,
                e.NombreCompleto,
                e.MetodoLecturaPreferido,
                e.Estado,
                Empresa = e.Cuenta.NombreComercial,
                CuentaId = e.CuentaId,
                TieneRostro = e.FaceTemplateHash != null
            })
            .ToListAsync();

        return Ok(empleados);
    }

    // 👆 ENDPOINT DE METODO DE LECTURA (para el Kiosko)
    [HttpGet("metodo-lectura/{numeroEmpleado}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerMetodoLectura(string numeroEmpleado, [FromQuery] string tokenKiosko)
    {
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(tokenKiosko)));
        
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && p.EstadoLicencia != "Suspendido");

        if (puntoLectura == null)
            return Unauthorized(new { mensaje = "Kiosko no autorizado." });

        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == numeroEmpleado && e.CuentaId == puntoLectura.CuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado." });

        return Ok(new
        {
            numeroEmpleado = empleado.NumeroEmpleado,
            nombre = empleado.NombreCompleto,
            metodoLectura = empleado.MetodoLecturaPreferido
        });
    }

    // 👆 VALIDAR PIN (para el Kiosko) — Solo para método ID+PIN
    public class ValidarPinRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    [HttpPost("validar-pin")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidarPin([FromBody] ValidarPinRequest request)
    {
        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado 
                                  && e.PinRespaldo == request.Pin 
                                  && e.Estado == "Activo");

        if (empleado == null)
            return Unauthorized(new { mensaje = "ID o PIN incorrecto." });

        return Ok(new
        {
            numeroEmpleado = empleado.NumeroEmpleado,
            nombre = empleado.NombreCompleto,
            metodoLectura = empleado.MetodoLecturaPreferido
        });
    }

    public class EditarEmpleadoRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string PinRespaldo { get; set; } = string.Empty;
        public string MetodoLecturaPreferido { get; set; } = "QR";
        public string Estado { get; set; } = "Activo";
        public string? FotoBase64 { get; set; }
    }

    [HttpPut("{empleadoId}")]
    public async Task<IActionResult> EditarEmpleado(Guid empleadoId, [FromBody] EditarEmpleadoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.CuentaId == cuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado en tu cuenta." });

        // 🛡️ Validar que el método de lectura sea canónico
        if (!MetodosValidos.Contains(request.MetodoLecturaPreferido))
        {
            return BadRequest(new
            {
                mensaje = $"Método de lectura inválido: '{request.MetodoLecturaPreferido}'. " +
                          $"Valores permitidos: {string.Join(", ", MetodosValidos)}"
            });
        }

        // 🛡️ Validar PIN único por cuenta (excluyendo el empleado actual)
        if (!string.IsNullOrWhiteSpace(request.PinRespaldo))
        {
            var pinExiste = await _db.Empleados
                .AnyAsync(e => e.CuentaId == cuentaId 
                            && e.PinRespaldo == request.PinRespaldo 
                            && e.EmpleadoId != empleadoId);

            if (pinExiste)
                return BadRequest(new { mensaje = "El PIN ya está en uso por otro empleado. Elige uno diferente." });
        }

        empleado.NombreCompleto = request.NombreCompleto;
        empleado.PinRespaldo = request.PinRespaldo;
        empleado.MetodoLecturaPreferido = request.MetodoLecturaPreferido;
        empleado.Estado = request.Estado;

        // 👆 SI SE ENVÍA NUEVA FOTO, REGENERAR EMBEDDING
        if (!string.IsNullOrEmpty(request.FotoBase64))
        {
            var embedding = _facialService.GenerarEmbedding(request.FotoBase64);
            if (embedding.Length > 0)
            {
                empleado.FaceTemplateHash = string.Join(",", embedding);
            }
            else
            {
                return BadRequest(new { mensaje = "No se pudo detectar el rostro en la foto enviada. Intenta con una foto de frente y bien iluminada." });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Empleado actualizado correctamente.",
            empleadoId,
            tieneRostro = !string.IsNullOrEmpty(empleado.FaceTemplateHash)
        });
    }

    [HttpDelete("{empleadoId}")]
    public async Task<IActionResult> EliminarEmpleado(Guid empleadoId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var empleado = await _db.Empleados
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId && e.CuentaId == cuentaId);

        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado en tu cuenta." });

        var tieneChecadas = await _db.RegistrosJornada
            .AnyAsync(r => r.EmpleadoId == empleadoId);

        if (tieneChecadas)
            return BadRequest(new { mensaje = "No se puede eliminar el empleado porque tiene checadas registradas." });

        var acuerdos = await _db.AcuerdosFirmados
            .Where(a => a.EmpleadoId == empleadoId)
            .ToListAsync();
        _db.AcuerdosFirmados.RemoveRange(acuerdos);

        _db.Empleados.Remove(empleado);
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado eliminado correctamente." });
    }
}
