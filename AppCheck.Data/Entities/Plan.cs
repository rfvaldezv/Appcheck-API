namespace AppCheck.Data.Entities;

public class Plan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public int LimiteEmpleados { get; set; }
    public int LimiteKioskos { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
}
