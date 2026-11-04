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
        public string? FotoBase64 { get; set; } // 👈 NUEVO CAMPO
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
            FaceTemplateHash = request.FotoBase64, // 👈 GUARDAR LA FOTO
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

        var empleados = await _db.Empleados
            .Where(e => e.CuentaId == cuentaId)
            .Select(e => new
            {
                e.EmpleadoId,
                e.NumeroEmpleado,
                e.NombreCompleto,
                e.MetodoLecturaPreferido,
                e.Estado
            })
            .ToListAsync();

        return Ok(empleados);
    }

    public class EditarEmpleadoRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string PinRespaldo { get; set; } = string.Empty;
        public string MetodoLecturaPreferido { get; set; } = "Mix";
        public string Estado { get; set; } = "Activo";
        public string? FotoBase64 { get; set; } // 👈 NUEVO CAMPO
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
        
        // Si se envía una nueva foto, actualizarla
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
            return BadRequest(new { mensaje = "No se puede eliminar el empleado porque tiene checadas registradas. Usa 'Inactivar' en su lugar." });

        var acuerdos = await _db.AcuerdosFirmados
            .Where(a => a.EmpleadoId == empleadoId)
            .ToListAsync();
        _db.AcuerdosFirmados.RemoveRange(acuerdos);

        _db.Empleados.Remove(empleado);
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado eliminado correctamente." });
    }
}
