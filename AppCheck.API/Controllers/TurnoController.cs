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

        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarTurno([FromBody] AsignarTurnoRequest req)
        {
            var empleado = await _db.Empleados.FindAsync(req.EmpleadoId);
            if (empleado == null) return NotFound(new { mensaje = "Empleado no encontrado" });

            // Cerrar asignación anterior si existe
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
