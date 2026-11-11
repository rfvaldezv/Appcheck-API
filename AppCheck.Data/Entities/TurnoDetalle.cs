using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCheck.Data.Entities
{
    [Table("TurnoDetalle")]
    public class TurnoDetalle
    {
        [Key]
        public Guid TurnoDetalleId { get; set; }
        public Guid TurnoId { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public int ToleranciaMin { get; set; } = 10;
        public bool EsLaborable { get; set; } = true;

        public Turno? Turno { get; set; }
    }
}
