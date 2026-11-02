namespace AppCheck.Data.Entities;

public class BitacoraAuditoria
{
    public Guid LogId { get; set; } = Guid.NewGuid();
    public Guid CuentaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
