using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;
using AppCheck.API.DTOs;

namespace AppCheck.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TurnoController : ControllerBase
    {
        private readonly AppCheckDbContext _db;
        public TurnoController(AppCheckDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var turnos = await _db.Turnos
                .Include(t => t.Detalles)
                .Where(t => t.CuentaId == cuentaId && t.Activo)
                .Select(t => new {
                    t.TurnoId,
                    t.Nombre,
                    t.ColorHex,
                    Detalles = t.Detalles.OrderBy(d => d.DiaSemana).Select(d => new {
                        d.DiaSemana, d.HoraEntrada, d.HoraSalida, d.ToleranciaMin, d.EsLaborable
                    })
                })
                .ToListAsync();
            return Ok(turnos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Obtener(Guid id)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var turno = await _db.Turnos
                .Include(t => t.Detalles)
                .FirstOrDefaultAsync(t => t.TurnoId == id && t.CuentaId == cuentaId);

            if (turno == null) return NotFound(new { mensaje = "Turno no encontrado." });

            return Ok(new {
                turno.TurnoId,
                turno.Nombre,
                turno.ColorHex,
                Detalles = turno.Detalles.OrderBy(d => d.DiaSemana).Select(d => new {
                    d.DiaSemana, d.HoraEntrada, d.HoraSalida, d.ToleranciaMin, d.EsLaborable
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearTurnoRequest req)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var nuevoTurno = new Turno
            {
                TurnoId = Guid.NewGuid(),
                CuentaId = cuentaId,
                Nombre = req.Nombre,
                ColorHex = req.ColorHex,
                Detalles = req.Detalles.Select(d => new TurnoDetalle
                {
                    TurnoDetalleId = Guid.NewGuid(),
                    DiaSemana = d.DiaSemana,
                    HoraEntrada = d.HoraEntrada,
                    HoraSalida = d.HoraSalida,
                    ToleranciaMin = d.ToleranciaMin,
                    EsLaborable = d.EsLaborable
                }).ToList()
            };

            _db.Turnos.Add(nuevoTurno);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Turno creado", turnoId = nuevoTurno.TurnoId });
        }

	[HttpPut("{id}")]
	public async Task<IActionResult> Editar(Guid id, [FromBody] CrearTurnoRequest req)
	{
	    var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
    
	    // 1. Buscar el turno SIN incluir los detalles (para evitar tracking innecesario)
	    var turno = await _db.Turnos
        	.FirstOrDefaultAsync(t => t.TurnoId == id && t.CuentaId == cuentaId);

	    if (turno == null) return NotFound(new { mensaje = "Turno no encontrado." });

	    // 2. Actualizar solo las propiedades escalares del turno
	    turno.Nombre = req.Nombre;
	    turno.ColorHex = req.ColorHex;

	    // 3. BORRAR los detalles viejos DIRECTAMENTE en la BD (sin cargarlos en memoria)
	    //    Esto evita que el Change Tracker de EF se confunda.
	    await _db.TurnoDetalles
	        .Where(d => d.TurnoId == id)
	        .ExecuteDeleteAsync();
	
	    // 4. Guardar el turno (con los detalles ya borrados) para "limpiar" el estado
	    await _db.SaveChangesAsync();
	
	    // 5. Ahora crear los nuevos detalles como entidades completamente nuevas
	    var nuevosDetalles = req.Detalles.Select(d => new TurnoDetalle
	    {
	        TurnoDetalleId = Guid.NewGuid(),
	        TurnoId = id,
	        DiaSemana = d.DiaSemana,
	        HoraEntrada = d.HoraEntrada,
	        HoraSalida = d.HoraSalida,
	        ToleranciaMin = d.ToleranciaMin,
	        EsLaborable = d.EsLaborable
	    }).ToList();
	
	    _db.TurnoDetalles.AddRange(nuevosDetalles);

	    // 6. Guardar los nuevos detalles
	    await _db.SaveChangesAsync();

	    return Ok(new { mensaje = "Turno actualizado correctamente." });
	}

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var cuentaId = Guid.Parse(User.FindFirst("CuentaId")?.Value!);
            var turno = await _db.Turnos.FirstOrDefaultAsync(t => t.TurnoId == id && t.CuentaId == cuentaId);

            if (turno == null) return NotFound(new { mensaje = "Turno no encontrado." });

            var tieneEmpleados = await _db.EmpleadoTurnos.AnyAsync(et => et.TurnoId == id && et.FechaFin == null);
            if (tieneEmpleados)
                return BadRequest(new { mensaje = "No se puede eliminar el turno porque está asignado a empleados activos." });

            turno.Activo = false;
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Turno eliminado correctamente." });
        }

        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarTurno([FromBody] AsignarTurnoRequest req)
        {
            var empleado = await _db.Empleados.FindAsync(req.EmpleadoId);
            if (empleado == null) return NotFound(new { mensaje = "Empleado no encontrado" });

            var asignacionActiva = await _db.EmpleadoTurnos
                .FirstOrDefaultAsync(et => et.EmpleadoId == req.EmpleadoId && et.FechaFin == null);
            
            if (asignacionActiva != null)
            {
                asignacionActiva.FechaFin = req.FechaInicio.AddDays(-1);
            }

            var nuevaAsignacion = new EmpleadoTurno
            {
                EmpleadoTurnoId = Guid.NewGuid(),
                EmpleadoId = req.EmpleadoId,
                TurnoId = req.TurnoId,
                FechaInicio = req.FechaInicio,
                FechaFin = req.FechaFin
            };

            _db.EmpleadoTurnos.Add(nuevaAsignacion);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Turno asignado correctamente" });
        }
    }
}
