using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppCheck.Data;
using AppCheck.Data.Entities;

namespace AppCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacturacionController : ControllerBase
{
    private readonly AppCheckDbContext _db;

    public FacturacionController(AppCheckDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // 1. LISTAR PLANES (público)
    // =====================================================
    [HttpGet("planes")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarPlanes()
    {
        var planes = await _db.Planes
            .Where(p => p.Activo)
            .OrderBy(p => p.PrecioBase)
            .Select(p => new
            {
                p.PlanId,
                p.Nombre,
                p.PrecioBase,
                p.LimiteEmpleados,
                p.LimiteKioskos
            })
            .ToListAsync();

        return Ok(planes);
    }

    // =====================================================
    // 2. OBTENER SUSCRIPCIÓN DE LA CUENTA (protegido)
    // =====================================================
    [HttpGet("mi-suscripcion")]
    [Authorize]
    public async Task<IActionResult> MiSuscripcion()
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var suscripcion = await _db.Suscripciones
            .Include(s => s.Plan)
            .Where(s => s.CuentaId == cuentaId && s.Estado == "Activa")
            .Select(s => new
            {
                s.SuscripcionId,
                s.FechaInicio,
                s.Estado,
                Plan = new
                {
                    s.Plan.PlanId,
                    s.Plan.Nombre,
                    s.Plan.PrecioBase,
                    s.Plan.LimiteEmpleados,
                    s.Plan.LimiteKioskos
                }
            })
            .FirstOrDefaultAsync();

        if (suscripcion == null)
            return Ok(new { mensaje = "No tienes una suscripción activa.", suscripcion = (object?)null });

        return Ok(suscripcion);
    }

    // =====================================================
    // 3. ASIGNAR PLAN A CUENTA (solo SuperAdmin)
    // =====================================================
    public class AsignarPlanRequest
    {
        public Guid CuentaId { get; set; }
        public Guid PlanId { get; set; }
    }

    [HttpPost("asignar-plan")]
    [Authorize]
    public async Task<IActionResult> AsignarPlan([FromBody] AsignarPlanRequest request)
    {
        // Solo SuperAdmin puede hacer esto
        var rol = User.FindFirst("Rol")?.Value;
        if (rol != "SuperAdmin")
            return Forbid();

        // Verificar que la cuenta y el plan existan
        var cuenta = await _db.Cuentas.FindAsync(request.CuentaId);
        if (cuenta == null)
            return NotFound(new { mensaje = "Cuenta no encontrada." });

        var plan = await _db.Planes.FindAsync(request.PlanId);
        if (plan == null)
            return NotFound(new { mensaje = "Plan no encontrado." });

        // Cancelar suscripciones anteriores
        var anteriores = await _db.Suscripciones
            .Where(s => s.CuentaId == request.CuentaId && s.Estado == "Activa")
            .ToListAsync();
        foreach (var ant in anteriores)
        {
            ant.Estado = "Cancelada";
            ant.FechaFin = DateTimeOffset.UtcNow;
        }

        // Crear nueva suscripción
        var nuevaSuscripcion = new Suscripcion
        {
            CuentaId = request.CuentaId,
            PlanId = request.PlanId,
            FechaInicio = DateTimeOffset.UtcNow,
            Estado = "Activa"
        };
        _db.Suscripciones.Add(nuevaSuscripcion);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Plan asignado correctamente.",
            suscripcionId = nuevaSuscripcion.SuscripcionId,
            plan = plan.Nombre
        });
    }

    // =====================================================
    // 4. LISTAR FACTURAS DE LA CUENTA (protegido)
    // =====================================================
    [HttpGet("facturas")]
    [Authorize]
    public async Task<IActionResult> ListarFacturas()
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var facturas = await _db.Facturas
            .Where(f => f.CuentaId == cuentaId)
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => new
            {
                f.FacturaId,
                f.PeriodoInicio,
                f.PeriodoFin,
                f.Subtotal,
                f.IVA,
                f.Total,
                f.Estado,
                f.FechaEmision,
                f.FechaPago,
                f.UUID_CFDI
            })
            .ToListAsync();

        return Ok(facturas);
    }

    // =====================================================
    // 5. GENERAR FACTURA DEL MES ACTUAL (protegido)
    // =====================================================
    [HttpPost("facturas/generar")]
    [Authorize]
    public async Task<IActionResult> GenerarFactura()
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        // 1. Obtener la suscripción activa
        var suscripcion = await _db.Suscripciones
            .Include(s => s.Plan)
            .Where(s => s.CuentaId == cuentaId && s.Estado == "Activa")
            .FirstOrDefaultAsync();

        if (suscripcion == null)
            return BadRequest(new { mensaje = "No tienes una suscripción activa." });

        // 2. Verificar si ya existe factura para este mes
        var inicioMes = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var finMes = inicioMes.AddMonths(1);

        var facturaExistente = await _db.Facturas
            .AnyAsync(f => f.CuentaId == cuentaId && f.PeriodoInicio == inicioMes);

        if (facturaExistente)
            return BadRequest(new { mensaje = "Ya existe una factura para este mes." });

        // 3. Calcular montos (basado en el plan)
        var subtotal = suscripcion.Plan.PrecioBase;
        var iva = subtotal * 0.16m; // IVA 16%
        var total = subtotal + iva;

        // 4. Crear la factura
        var factura = new Factura
        {
            CuentaId = cuentaId,
            SuscripcionId = suscripcion.SuscripcionId,
            PeriodoInicio = inicioMes,
            PeriodoFin = finMes,
            Subtotal = subtotal,
            IVA = iva,
            Total = total,
            Estado = "Emitida",
            FechaEmision = DateTimeOffset.UtcNow
        };

        _db.Facturas.Add(factura);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Factura generada correctamente.",
            facturaId = factura.FacturaId,
            subtotal,
            iva,
            total,
            periodo = $"{inicioMes:MMMM yyyy}"
        });
    }

    // =====================================================
    // 6. CALCULAR COMISIÓN DEL VENDEDOR (protegido)
    // =====================================================
    [HttpGet("comisiones")]
    [Authorize]
    public async Task<IActionResult> MisComisiones()
    {
        var usuarioIdClaim = User.FindFirst("sub")?.Value;
        var rol = User.FindFirst("Rol")?.Value;

        if (string.IsNullOrEmpty(usuarioIdClaim))
            return Unauthorized();

        if (rol != "Vendedor")
            return Forbid(); // Solo vendedores pueden ver sus comisiones

        var vendedorId = Guid.Parse(usuarioIdClaim);

        // Configuración de comisión del vendedor
        var config = await _db.ConfiguracionesComision
            .FirstOrDefaultAsync(c => c.VendedorId == vendedorId && c.VigenteHasta == null);

        if (config == null)
            return NotFound(new { mensaje = "No tienes configuración de comisiones." });

        // Clientes del vendedor
        var clientes = await _db.Cuentas
            .Where(c => c.VendedorId == vendedorId)
            .Select(c => c.CuentaId)
            .ToListAsync();

        // MRR de esos clientes (suma de suscripciones activas)
        var mrr = await _db.Suscripciones
            .Include(s => s.Plan)
            .Where(s => clientes.Contains(s.CuentaId) && s.Estado == "Activa")
            .SumAsync(s => s.Plan.PrecioBase);

        var comision = mrr * config.PorcentajeComisionRecurrente;

        return Ok(new
        {
            vendedorId,
            clientesActivos = clientes.Count,
            mrrCobrado = mrr,
            porcentajeComision = config.PorcentajeComisionRecurrente,
            comisionCalculada = comision,
            sueldoBase = config.SueldoBaseMensual ?? 0,
            totalEstimado = comision + (config.SueldoBaseMensual ?? 0)
        });
    }

    // =====================================================
    // 7. MARCAR FACTURA COMO PAGADA (protegido)
    // =====================================================
    [HttpPost("facturas/{facturaId}/pagar")]
    [Authorize]
    public async Task<IActionResult> MarcarFacturaPagada(Guid facturaId)
    {
        var cuentaIdClaim = User.FindFirst("CuentaId")?.Value;
        if (string.IsNullOrEmpty(cuentaIdClaim))
            return Unauthorized();

        var cuentaId = Guid.Parse(cuentaIdClaim);

        var factura = await _db.Facturas
            .FirstOrDefaultAsync(f => f.FacturaId == facturaId && f.CuentaId == cuentaId);

        if (factura == null)
            return NotFound(new { mensaje = "Factura no encontrada." });

        if (factura.Estado == "Pagada")
            return BadRequest(new { mensaje = "La factura ya está pagada." });

        factura.Estado = "Pagada";
        factura.FechaPago = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { mensaje = "Factura marcada como pagada correctamente.", facturaId });
    }
}
