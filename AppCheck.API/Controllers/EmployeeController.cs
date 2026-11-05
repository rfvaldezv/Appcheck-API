using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public EmployeeController(AppCheckDbContext db)
    {
        _db = db;
    }

    public class AltaEmpleadoRequest
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string PinRespaldo { get; set; } = string.Empty;
        public string MetodoLecturaPreferido { get; set; } = "Mix";
        public string? FotoBase64 { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> AltaEmpleado([FromBody] AltaEmpleadoRequest request)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized(new { mensaje = "Token inválido o sin cuenta asociada." });

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var existe = await _db.Empleados
            .AnyAsync(e => e.CuentaId == cuentaId && e.NumeroEmpleado == request.NumeroEmpleado);

        if (existe)
            return BadRequest(new { mensaje = $"Ya existe un empleado con el número {request.NumeroEmpleado}." });

        var nuevoEmpleado = new Empleado
        {
            CuentaId = cuentaId,
            NumeroEmpleado = request.NumeroEmpleado,
            NombreCompleto = request.NombreCompleto,
            PinRespaldo = request.PinRespaldo,
            MetodoLecturaPreferido = request.MetodoLecturaPreferido,
            FaceTemplateHash = request.FotoBase64,
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

        // Si es SuperAdmin, ve todos los empleados. Si no, solo los de su cuenta.
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
                // 👈 Nombre del vendedor (si la cuenta tiene uno asignado)
                Vendedor = _db.Usuarios
                    .Where(u => u.UsuarioId == e.Cuenta.VendedorId)
                    .Select(u => u.Nombre)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(empleados);
    }

    // 👈 ENDPOINT DE METODO DE LECTURA (Corregido)
    [HttpGet("metodo-lectura/{numeroEmpleado}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerMetodoLectura(string numeroEmpleado, [FromQuery] string tokenKiosko)
    {
        // El token que envía el Kiosko YA es el hash guardado en la BD
        var puntoLectura = await _db.PuntosLectura
            .FirstOrDefaultAsync(p => p.TokenHash == tokenKiosko && p.EstadoLicencia != "Suspendido");

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

    public class EditarEmpleadoRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string PinRespaldo { get; set; } = string.Empty;
        public string MetodoLecturaPreferido { get; set; } = "Mix";
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

        empleado.NombreCompleto = request.NombreCompleto;
        empleado.PinRespaldo = request.PinRespaldo;
        empleado.MetodoLecturaPreferido = request.MetodoLecturaPreferido;
        empleado.Estado = request.Estado;

        if (!string.IsNullOrEmpty(request.FotoBase64))
        {
            empleado.FaceTemplateHash = request.FotoBase64;
        }

        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado actualizado correctamente.", empleadoId });
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

    [HttpGet("test-version")]
    [AllowAnonymous]
    public IActionResult TestVersion()
    {
        return Ok(new { 
            mensaje = "¡El DLL está actualizado!", 
            fecha = DateTimeOffset.UtcNow,
            version = "2.0.0"
        });
    }

    // =====================================================
    // 13. VALIDAR PIN (público, para el Kiosko)
    // =====================================================
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
            .FirstOrDefaultAsync(e => e.NumeroEmpleado == request.NumeroEmpleado && e.PinRespaldo == request.Pin && e.Estado == "Activo");

        if (empleado == null)
            return Unauthorized(new { mensaje = "PIN incorrecto o empleado inactivo." });

        return Ok(new
        {
            numeroEmpleado = empleado.NumeroEmpleado,
            nombre = empleado.NombreCompleto,
            metodoLectura = empleado.MetodoLecturaPreferido
        });
    }
}
