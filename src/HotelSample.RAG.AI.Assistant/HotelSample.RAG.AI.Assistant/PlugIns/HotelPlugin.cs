using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HotelSample.RAG.AI.Assistant.PlugIns
{
    public class HotelPlugin
    {
        [KernelFunction("create_booking")]
        [Description("Crea una reserva para el hotel. Esta funcion se va a ejecutar cuando se confirme la reserva")]
        public object CreateBooking(
            [Description("Name indica el nombre de la persona que realiza la reserva.")] string name,
            [Description("Persons es el numero entero ingresado por el usuario que indica cuantas personas se van a alojar en el hotel")] int personas,
            [Description("DateFrom indica la fecha en la que el usuario ingresa al hotel. Converti la fecha que ingrese el usuario al formato dd-MM-yyyy.")] string dateFrom,
            [Description("DateTo indica la fecha en la que el usuario abandona el hotel. Converti la fecha que ingrese el usuario al formato dd-MM-yyyy.")] string dateTo)
        {

            //Simulacion de llamada Http
            return new
            {
                Success = true,
                BookingCode = "JFS356"
            };
        }
    }
}
