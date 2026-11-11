namespace AppCheck.Data.Entities;

public class Empleado
{
    public Guid EmpleadoId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public string NumeroEmpleado { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PinRespaldo { get; set; } = string.Empty;
    public string MetodoLecturaPreferido { get; set; } = "Mix";
    public string? FaceTemplateHash { get; set; }
    public string Estado { get; set; } = "Activo";
    public bool PuedeUsarHuella { get; set; } = false;  // Admin decide

    // 👇 NUEVO: Relación con Sucursal 👇
    public Guid? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public Cuenta Cuenta { get; set; } = null!;
    public ICollection<RegistroJornada> RegistrosJornada { get; set; } = new List<RegistroJornada>();
    public ICollection<AcuerdoFirmado> AcuerdosFirmados { get; set; } = new List<AcuerdoFirmado>();
}
