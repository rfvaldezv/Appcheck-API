namespace AppCheck.Data.Entities;

public class Suscripcion
{
    public Guid SuscripcionId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public Guid PlanId { get; set; }
    public DateTimeOffset FechaInicio { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaFin { get; set; }
    public string Estado { get; set; } = "Activa"; // Activa, Suspendida, Cancelada

    public Cuenta Cuenta { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
