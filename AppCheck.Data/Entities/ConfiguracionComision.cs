namespace AppCheck.Data.Entities;

public class ConfiguracionComision
{
    public Guid ConfiguracionId { get; set; } = Guid.NewGuid();
    public Guid VendedorId { get; set; }
    public decimal? SueldoBaseMensual { get; set; }
    public decimal PorcentajeComisionRecurrente { get; set; } = 0.20m;
    public decimal? BonoPorNuevoCliente { get; set; }
    public decimal? BonoPorMeta { get; set; }
    public int? MetaClientesMes { get; set; }
    public DateTimeOffset VigenteDesde { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? VigenteHasta { get; set; }

    public Usuario Vendedor { get; set; } = null!;
}
