namespace AppCheck.Data.Entities;

public class AcuerdoFirmado
{
    public Guid AcuerdoId { get; set; } = Guid.NewGuid();
    public Guid EmpleadoId { get; set; }
    public string DocumentoUrl { get; set; } = string.Empty;
    public DateTimeOffset FechaFirma { get; set; } = DateTimeOffset.UtcNow;

    public Empleado Empleado { get; set; } = null!;
}
