namespace AppCheck.Data.Entities;

public class CodigoActivacion
{
    public Guid CodigoId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public Guid? PuntoLecturaId { get; set; }
    public string CodigoHash { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Activacion";
    public string Estado { get; set; } = "Pendiente";
    public DateTimeOffset ExpiraEn { get; set; }

    public Cuenta Cuenta { get; set; } = null!;
    public PuntoLectura? PuntoLectura { get; set; }
}
