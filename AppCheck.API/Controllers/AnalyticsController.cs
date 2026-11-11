using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;

namespace AppCheck.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppCheckDbContext _db;
        public AnalyticsController(AppCheckDbContext db) => _db = db;

        // =====================================================
        // 1. DASHBOARD (El que se perdió)
        // =====================================================
        [HttpGet("dashboard")]
        public async Task<IActionResult> ObtenerDashboard(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
            if (string.IsNullOrEmpty(cuentaIdClaim)) return Unauthorized();

            var cuentaId = Guid.Parse(cuentaIdClaim);
            var rol = User.FindFirst("Rol")?.Value;

            var inicio = (fechaInicio ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Date;
            var fin = (fechaFin ?? DateTime.UtcNow).Date;

            // 1. Checadas del período
            var queryChecadas = _db.RegistrosJornada
                .Include(r => r.Empleado)
                .Where(r => r.TimestampServidor.Date >= inicio && r.TimestampServidor.Date <= fin);

            if (rol != "SuperAdmin") queryChecadas = queryChecadas.Where(r => r.Empleado.CuentaId == cuentaId);
            var checadasList = await queryChecadas.ToListAsync();

            // 2. Empleados
            var queryEmpleados = _db.Empleados.Where(e => e.Estado == "Activo");
            if (rol != "SuperAdmin") queryEmpleados = queryEmpleados.Where(e => e.CuentaId == cuentaId);
            var totalEmpleados = await queryEmpleados.CountAsync();

            // 3. Puntualidad
            var entradas = checadasList.Where(r => r.TipoChecada == "Entrada").ToList();
            int puntuales = 0;
            int retardos = 0;
            var empleadosIds = entradas.Select(e => e.EmpleadoId).Distinct().ToList();
            var turnosAsignados = await _db.EmpleadoTurnos
                .Include(et => et.Turno).ThenInclude(t => t.Detalles)
                .Where(et => empleadosIds.Contains(et.EmpleadoId))
                .ToListAsync();

            foreach (var checada in entradas)
            {
                int diaSemana = (int)checada.TimestampServidor.DayOfWeek;
                if (diaSemana == 0) diaSemana = 7;
                var turno = turnosAsignados.FirstOrDefault(t => t.EmpleadoId == checada.EmpleadoId 
                    && t.FechaInicio <= checada.TimestampServidor.Date 
                    && (t.FechaFin == null || t.FechaFin >= checada.TimestampServidor.Date));
                if (turno != null)
                {
                    var regla = turno.Turno?.Detalles.FirstOrDefault(d => d.DiaSemana == diaSemana);
                    if (regla != null && regla.EsLaborable)
                    {
                        var horaEsperada = checada.TimestampServidor.Date.Add(regla.HoraEntrada);
                        var tolerancia = TimeSpan.FromMinutes(regla.ToleranciaMin);
                        if (checada.TimestampServidor <= horaEsperada + tolerancia) puntuales++;
                        else retardos++;
                    }
                }
            }

            var porcentajePuntuales = entradas.Count > 0 ? (puntuales * 100 / entradas.Count) : 0;
            var porcentajeRetardos = entradas.Count > 0 ? (retardos * 100 / entradas.Count) : 0;
            var empleadosConChecada = checadasList.Select(r => r.EmpleadoId).Distinct().Count();
            var ausencias = totalEmpleados - empleadosConChecada;
            if (ausencias < 0) ausencias = 0;

            // 4. Gráficos (Checadas por día)
            var checadasPorDia = checadasList
                .GroupBy(r => r.TimestampServidor.Date)
                .Select(g => new { fecha = g.Key.ToString("yyyy-MM-dd"), total = g.Count() })
                .OrderBy(x => x.fecha)
                .ToList();

            // 5. Gráficos (Métodos usados)
            var metodosUsados = checadasList
                .GroupBy(r => r.MetodoUsado ?? "Manual")
                .Select(g => new { metodo = g.Key, total = g.Count() })
                .ToList();

            // 6. Devolver formato esperado por el Frontend
            return Ok(new
            {
                periodo = new
                {
                    inicio = inicio.ToString("yyyy-MM-dd"),
                    fin = fin.ToString("yyyy-MM-dd")
                },
                kpis = new
                {
                    totalChecadas = checadasList.Count,
                    totalEmpleados = totalEmpleados,
                    porcentajePuntuales,
                    porcentajeRetardos,
                    horasExtra = 0,
                    ausencias,
                    totalSucursales = await _db.Sucursales.CountAsync(s => s.Activa),
                    totalKioskos = await _db.PuntosLectura.CountAsync(p => p.EstadoLicencia == "Activa")
                },
                graficos = new
                {
                    checadasPorDia,
                    metodosUsados
                }
            });
        }

        // =====================================================
        // 2. ASISTENCIA POR PERIODO (El nuevo)
        // =====================================================
        [HttpGet("asistencia")]
        public async Task<IActionResult> AsistenciaPorPeriodo(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] Guid? cuentaId = null,
            [FromQuery] Guid? sucursalId = null,
            [FromQuery] Guid? empleadoId = null)
        {
            var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
            if (string.IsNullOrEmpty(cuentaIdClaim)) return Unauthorized();

            var cuentaIdToken = Guid.Parse(cuentaIdClaim);
            var rol = User.FindFirst("Rol")?.Value;

            var inicio = (fechaInicio ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Date;
            var fin = (fechaFin ?? DateTime.UtcNow).Date;

            if (fin < inicio) return BadRequest(new { mensaje = "La fecha fin no puede ser menor a la fecha inicio." });

            var queryEmpleados = _db.Empleados.Where(e => e.Estado == "Activo");
            
            if (rol != "SuperAdmin") 
                queryEmpleados = queryEmpleados.Where(e => e.CuentaId == cuentaIdToken);
            else if (cuentaId.HasValue) 
                queryEmpleados = queryEmpleados.Where(e => e.CuentaId == cuentaId.Value);

            if (sucursalId.HasValue) queryEmpleados = queryEmpleados.Where(e => e.SucursalId == sucursalId.Value);
            if (empleadoId.HasValue) queryEmpleados = queryEmpleados.Where(e => e.EmpleadoId == empleadoId.Value);

            var empleados = await queryEmpleados.ToListAsync();
            var resultado = new List<object>();

            for (var dia = inicio; dia <= fin; dia = dia.AddDays(1))
            {
                int diaSemana = (int)dia.DayOfWeek;
                if (diaSemana == 0) diaSemana = 7;

                foreach (var emp in empleados)
                {
                    var turnoAsignado = await _db.EmpleadoTurnos
                        .Include(et => et.Turno).ThenInclude(t => t.Detalles)
                        .FirstOrDefaultAsync(et => et.EmpleadoId == emp.EmpleadoId 
                                                && et.FechaInicio <= dia 
                                                && (et.FechaFin == null || et.FechaFin >= dia));

                    string estatus = "Sin Turno";
                    string? horaEntradaTurno = null;
                    string? horaEntradaReal = null;
                    int minutosRetardo = 0;

                    if (turnoAsignado != null && turnoAsignado.Turno != null)
                    {
                        var reglaDia = turnoAsignado.Turno.Detalles.FirstOrDefault(d => d.DiaSemana == diaSemana);
                        
                        if (reglaDia != null && reglaDia.EsLaborable)
                        {
                            horaEntradaTurno = reglaDia.HoraEntrada.ToString(@"hh\:mm");
                            
                            var checada = await _db.RegistrosJornada
                                .Where(r => r.EmpleadoId == emp.EmpleadoId 
                                         && r.TipoChecada == "Entrada"
                                         && r.TimestampServidor.Date == dia)
                                .OrderBy(r => r.TimestampServidor)
                                .FirstOrDefaultAsync();

                            if (checada == null)
                            {
                                estatus = "Falta";
                            }
                            else
                            {
                                horaEntradaReal = checada.TimestampServidor.ToString("HH:mm");
                                var horaEsperada = dia.Add(reglaDia.HoraEntrada);
                                var tolerancia = TimeSpan.FromMinutes(reglaDia.ToleranciaMin);

                                if (checada.TimestampServidor <= horaEsperada + tolerancia) estatus = "Puntual";
                                else
                                {
                                    estatus = "Retardo";
                                    minutosRetardo = (int)(checada.TimestampServidor - horaEsperada).TotalMinutes;
                                }
                            }
                        }
                        else if (reglaDia != null && !reglaDia.EsLaborable) estatus = "Día Descanso";
                    }

                    if (estatus != "Día Descanso" && estatus != "Sin Turno")
                    {
                        resultado.Add(new
                        {
                            fecha = dia.ToString("yyyy-MM-dd"),
                            diaSemana = diaSemana,
                            empleadoId = emp.EmpleadoId,
                            numeroEmpleado = emp.NumeroEmpleado,
                            nombreCompleto = emp.NombreCompleto,
                            sucursalId = emp.SucursalId,
                            turnoNombre = turnoAsignado?.Turno?.Nombre ?? "Ninguno",
                            horaEntradaTurno,
                            horaEntradaReal,
                            estatus,
                            minutosRetardo
                        });
                    }
                }
            }

            return Ok(new
            {
                fechaInicio = inicio.ToString("yyyy-MM-dd"),
                fechaFin = fin.ToString("yyyy-MM-dd"),
                resumen = new
                {
                    totalRegistros = resultado.Count,
                    puntuales = resultado.Count(r => ((dynamic)r).estatus == "Puntual"),
                    retardos = resultado.Count(r => ((dynamic)r).estatus == "Retardo"),
                    faltas = resultado.Count(r => ((dynamic)r).estatus == "Falta"),
                },
                detalle = resultado
            });
        }
    }
}
