namespace AppCheck.Data.Entities;

public class PuntoLectura
{
    public Guid PuntoLecturaId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string EstadoLicencia { get; set; } = "Activa";
    public DateTimeOffset? FechaExpiracionLicencia { get; set; }
    public DateTimeOffset? UltimoHeartbeat { get; set; }

    public Cuenta Cuenta { get; set; } = null!;
    public ICollection<RegistroJornada> RegistrosJornada { get; set; } = new List<RegistroJornada>();
}
