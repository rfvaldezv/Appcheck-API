using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCheck.Data.Entities
{
    [Table("Sucursales")]
    public class Sucursal
    {
        [Key]
        public Guid SucursalId { get; set; }
        public Guid CuentaId { get; set; }
        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Direccion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int RadioMetros { get; set; } = 100;
        public bool Activa { get; set; } = true;
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        public Cuenta? Cuenta { get; set; }
    }
}
