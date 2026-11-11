using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCheck.Data.Entities
{
    [Table("EmpleadoTurnos")]
    public class EmpleadoTurno
    {
        [Key]
        public Guid EmpleadoTurnoId { get; set; }
        public Guid EmpleadoId { get; set; }
        public Guid TurnoId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public Empleado? Empleado { get; set; }
        public Turno? Turno { get; set; }
    }
}
