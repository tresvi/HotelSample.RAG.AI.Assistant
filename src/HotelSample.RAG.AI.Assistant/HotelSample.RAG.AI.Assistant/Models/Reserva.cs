using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Models
{
    internal class Reserva
    {
        public string? BookingCode { get; set; }
        public string? Nombre { get; set; } = "";
        public string? FechaDesde { get; set; }
        public string? FechaHasta { get; set; }
        public int CantidadDePersonas { get; set; }

        public override string ToString()
        {
            string respuesta = $"BookingCode:{BookingCode} ";
            respuesta += $"Nombre:{Nombre} ";
            respuesta += $"Fecha Desde:{FechaDesde} ";
            respuesta += $"Fecha Hasta:{FechaHasta} ";
            respuesta += $"Cantidad de personas:{CantidadDePersonas}";

            return respuesta;
        }
    }
}
