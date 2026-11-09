namespace AppCheck.Data.Entities;

public class RegistroJornada
{
    public Guid RegistroId { get; set; } = Guid.NewGuid();
    public Guid EmpleadoId { get; set; }
    public Guid? PuntoLecturaId { get; set; }
    public string TipoChecada { get; set; } = string.Empty;
    public DateTimeOffset TimestampServidor { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset TimestampLocal { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string MetodoUsado { get; set; } = string.Empty;
    public string HashRegistro { get; set; } = string.Empty;
    public bool EsOffline { get; set; } = false;

    public Empleado Empleado { get; set; } = null!;
    public PuntoLectura? PuntoLectura { get; set; } = null!;
}
