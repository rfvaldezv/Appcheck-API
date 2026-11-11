using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCheck.Data.Entities
{
    [Table("Turnos")]
    public class Turno
    {
        [Key]
        public Guid TurnoId { get; set; }
        public Guid CuentaId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        [MaxLength(7)]
        public string ColorHex { get; set; } = "#4CAF50";
        public bool Activo { get; set; } = true;
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        public Cuenta? Cuenta { get; set; }
        public ICollection<TurnoDetalle> Detalles { get; set; } = new List<TurnoDetalle>();
    }
}
