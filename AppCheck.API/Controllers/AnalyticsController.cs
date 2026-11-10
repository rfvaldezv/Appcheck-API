using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public AnalyticsController(AppCheckDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // DASHBOARD CON KPIs
    // =====================================================
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] DateTimeOffset? fechaInicio,
        [FromQuery] DateTimeOffset? fechaFin)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);
        var rol = User.FindFirst("Rol")?.Value;

        // Default: mes actual
        var inicio = fechaInicio ?? new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var fin = fechaFin ?? DateTimeOffset.UtcNow;

        // Query base
        var query = _db.RegistrosJornada
            .Include(r => r.Empleado)
                .ThenInclude(e => e.Cuenta)
            .Where(r => r.TimestampServidor >= inicio && r.TimestampServidor <= fin);

        if (rol != "SuperAdmin")
        {
            query = query.Where(r => r.Empleado.CuentaId == cuentaId);
        }

        var registros = await query.ToListAsync();

        // KPIs
        var totalChecadas = registros.Count;
        var empleadosActivos = await _db.Empleados
            .Where(e => (rol == "SuperAdmin" || e.CuentaId == cuentaId) && e.Estado == "Activo")
            .CountAsync();

        // Empleados con al menos 1 checada
        var empleadosConChecada = registros
            .Select(r => r.EmpleadoId)
            .Distinct()
            .Count();

        // Retardos (simplificado: checadas después de las 9:00 AM)
        var retardos = registros
            .Count(r => r.TipoChecada == "Entrada" && r.TimestampServidor.Hour >= 9);

        var puntuales = totalChecadas - retardos;
        var porcentajePuntuales = totalChecadas > 0 ? Math.Round((double)puntuales / totalChecadas * 100, 1) : 0;
        var porcentajeRetardos = totalChecadas > 0 ? Math.Round((double)retardos / totalChecadas * 100, 1) : 0;

        // Horas extra (simplificado: checadas después de las 6:00 PM)
        var horasExtra = registros
            .Count(r => r.TipoChecada == "Salida" && r.TimestampServidor.Hour >= 18) * 1.0;

        // Ausencias (empleados activos - empleados con checada)
        var ausencias = Math.Max(0, empleadosActivos - empleadosConChecada);

        // Sucursales y kioskos
        var totalSucursales = 0; // TODO: cuando tengamos Sucursales
        var totalKioskos = await _db.PuntosLectura
            .Where(p => (rol == "SuperAdmin" || p.CuentaId == cuentaId) && p.EstadoLicencia == "Activa")
            .CountAsync();

        // Gráfico: checadas por día
        var checadasPorDia = registros
            .GroupBy(r => r.TimestampServidor.Date)
            .Select(g => new
            {
                fecha = g.Key.ToString("yyyy-MM-dd"),
                total = g.Count()
            })
            .OrderBy(x => x.fecha)
            .ToList();

        // Gráfico: métodos usados
        var metodosUsados = registros
            .GroupBy(r => r.MetodoUsado)
            .Select(g => new
            {
                metodo = g.Key,
                total = g.Count()
            })
            .OrderByDescending(x => x.total)
            .ToList();

        return Ok(new
        {
            periodo = new
            {
                inicio = inicio.ToString("yyyy-MM-dd"),
                fin = fin.ToString("yyyy-MM-dd")
            },
            kpis = new
            {
                totalChecadas,
                totalEmpleados = empleadosActivos,
                empleadosConChecada,
                porcentajePuntuales,
                porcentajeRetardos,
                horasExtra,
                ausencias,
                totalSucursales,
                totalKioskos
            },
            graficos = new
            {
                checadasPorDia,
                metodosUsados
            }
        });
    }
}
