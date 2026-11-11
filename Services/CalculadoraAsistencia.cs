using AppCheck.API.Models;

namespace AppCheck.API.Services
{
    public class CalculadoraAsistencia
    {
        public string DeterminarEstado(RegistroJornada registro, EmpleadoTurno turnoAsignado)
        {
            if (turnoAsignado == null || turnoAsignado.Turno?.Detalles == null) 
                return "Sin Turno";

            // Buscar la regla para el día de la semana (1=Lunes, 7=Domingo)
            int diaSemana = (int)registro.TimestampServidor.DayOfWeek;
            if (diaSemana == 0) diaSemana = 7; // En .NET Domingo=0, lo pasamos a 7

            var reglaDia = turnoAsignado.Turno.Detalles
                .FirstOrDefault(d => d.DiaSemana == diaSemana);

            if (reglaDia == null || !reglaDia.EsLaborable) 
                return "Día no laborable";

            if (registro.TipoChecada == "Entrada")
            {
                var horaEntradaEsperada = reglaDia.HoraEntrada;
                var tolerancia = TimeSpan.FromMinutes(reglaDia.ToleranciaMin);
                var horaReal = registro.TimestampServidor.LocalDateTime.TimeOfDay;

                if (horaReal <= horaEntradaEsperada + tolerancia)
                    return "Puntual";
                
                return "Retardo";
            }

            return "Checada válida";
        }
    }
}
