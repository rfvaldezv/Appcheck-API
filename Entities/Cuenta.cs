namespace AppCheck.Data.Entities;

public class Cuenta
{
    public Guid CuentaId { get; set; } = Guid.NewGuid();
    public Guid? CuentaPadreId { get; set; }
    public Guid? VendedorId { get; set; }
    public string TipoCuenta { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string? RFC { get; set; }
    public string Estado { get; set; } = "Activa";
    public bool ModoConfianza { get; set; } = false;
    public decimal SaldoActual { get; set; } = 0;
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public Cuenta? CuentaPadre { get; set; }
    public ICollection<Cuenta> CuentasHijas { get; set; } = new List<Cuenta>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<PuntoLectura> PuntosLectura { get; set; } = new List<PuntoLectura>();
}
