namespace AppCheck.Data.Entities;

public class Factura
{
    public Guid FacturaId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public Guid SuscripcionId { get; set; }  // 👈 Asegúrate de que este campo existe
    public DateTimeOffset PeriodoInicio { get; set; }
    public DateTimeOffset PeriodoFin { get; set; }
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    public string? UUID_CFDI { get; set; }
    public string Estado { get; set; } = "Emitida";
    public DateTimeOffset FechaEmision { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaPago { get; set; }
    public string? URL_PDF { get; set; }
    public string? URL_XML { get; set; }

    public Cuenta Cuenta { get; set; } = null!;
    public Suscripcion Suscripcion { get; set; } = null!;
}
