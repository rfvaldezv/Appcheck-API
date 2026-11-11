using System;
using System.Collections.Generic;

namespace AppCheck.API.DTOs
{
    // --- SUCURSALES ---
    public class CrearSucursalRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int RadioMetros { get; set; } = 100;
    }

    // --- TURNOS ---
    public class TurnoDetalleDto
    {
        public int DiaSemana { get; set; } // 1=Lunes, 7=Domingo
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public int ToleranciaMin { get; set; } = 10;
        public bool EsLaborable { get; set; } = true;
    }

    public class CrearTurnoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#4CAF50";
        public List<TurnoDetalleDto> Detalles { get; set; } = new();
    }

    // --- ASIGNACIÓN ---
    public class AsignarTurnoRequest
    {
        public Guid EmpleadoId { get; set; }
        public Guid TurnoId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}
