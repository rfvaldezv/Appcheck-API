namespace AppCheck.Data.Entities;

public class Liquidacion
{
    public Guid LiquidacionId { get; set; } = Guid.NewGuid();
    public Guid VendedorId { get; set; }
    public string Periodo { get; set; } = string.Empty; // Ej. "2026-11"
    public decimal MRRCobrado { get; set; }
    public decimal ComisionRecurrente { get; set; }
    public decimal Bonos { get; set; }
    public decimal SueldoBase { get; set; }
    public decimal TotalAPagar { get; set; }
    public string Estado { get; set; } = "Calculada"; // Calculada, Aceptada, Pagada
    public DateTimeOffset FechaCalculo { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaPago { get; set; }

    public Usuario Vendedor { get; set; } = null!;
}
