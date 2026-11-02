namespace AppCheck.Data.Entities;

public class Usuario
{
    public Guid UsuarioId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public Cuenta Cuenta { get; set; } = null!;
}
