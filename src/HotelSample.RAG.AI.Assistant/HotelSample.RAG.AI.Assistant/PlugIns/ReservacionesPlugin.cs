using HotelSample.RAG.AI.Assistant.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class ReservacionesPlugin
    {
        static List<Reserva> _reservaciones = new List<Reserva>();


        [KernelFunction("create_booking")]
        [Description("Crea una reserva para el hotel. Esta funcion se va a ejecutar cuando se confirme la reserva")]
        public object CreateBooking(
            [Description("Name indica el nombre de la persona que realiza la reserva.")] string name,
            [Description("Persons es el numero entero ingresado por el usuario que indica cuantas personas se van a alojar en el hotel")] int persons,
            [Description("DateFrom indica la fecha en la que el usuario ingresa al hotel. Converti la fecha que ingrese el usuario al formato dd-MM-yyyy.")] string dateFrom,
            [Description("DateTo indica la fecha en la que el usuario abandona el hotel. Converti la fecha que ingrese el usuario al formato dd-MM-yyyy.")] string dateTo)
        {
            string codigo = GenerarCodigoAlfanumerico(6);
            Reserva reserva = new Reserva()
            {
                BookingCode = codigo,
                Nombre = name,
                FechaDesde = dateFrom,
                FechaHasta = dateTo,
                CantidadDePersonas = persons
            };

            _reservaciones.Add(reserva);

            //Simulacion de llamada por ejemplo a una api rest o a una DB
            return new
            {
                Success = true,
                BookingCode = codigo
            };
        }


        [KernelFunction("delete_booking")]
        [Description("Elimina una reserva para el hotel sabiendo, buscandola por bookingCode. Esta funcion se va a ejecutar cuando se confirme una eliminación de una reserva. La eliminación siempre debe ser confirmada por el usuario")]
        public object DeleteBooking(
            [Description("BookingCode indica el codigo de la reserva a eliminar")] string bookingCode
            )
        {
            string msjeRespuesta;
            bool success;

            Reserva? reserva = _reservaciones.FirstOrDefault(r => r.BookingCode == bookingCode);

            if (reserva == null)
            {
                success = false;
                msjeRespuesta = $"No se encontró la reserva con el código {bookingCode}";
            }
            else
            {
                _reservaciones.Remove(reserva);
                success = true;
                msjeRespuesta = "Reserva eliminada correctamente.";
            }

            return new
            {
                Success = success,
                Message = msjeRespuesta
            };
        }


        [KernelFunction("List_By_Name")]
        [Description("Busca y devuelve las reservas que tiene una persona por nombre  Esta funcion se va a ejecutar cuando se confirme una eliminación de una reserva.")]
        public object ListByName(
            [Description("Name indica el nombre bajo el cual se hizo la reserva y por el cual se va a buscar")] string name
            )
        {
            var reservas = _reservaciones.Where(r => r.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            string msjeRespuesta = "";
            bool success = false;

            if (reservas.Any())
            {
                foreach (var reserva in reservas)
                {
                    success = true;
                    msjeRespuesta += $"- {reserva}";
                }
            }
            else
            {
                success = false;
                msjeRespuesta += "No se encontraron reservas para este nombre.";
            }

            return new
            {
                Success = success,
                Message = msjeRespuesta
            };
        }


        [KernelFunction("find_by_bookingcode")]
        [Description("Busca las reservas por BookingCode, como el bookingcode es irrepetible, " +
            "a lo sumo encontrará y devolverá una sola reserva.")]
        public object BuscarReservaPorCodigo(
            [Description("BookingCode es el codigo de reserva por el cual se realizará la busqueda, no puede ser nulo")]string bookingCode
            )
        {
            string msjeRespuesta;
            bool success;

            Reserva? reserva = _reservaciones.FirstOrDefault(r => r.BookingCode == bookingCode);

            if (reserva == null)
            {
                success = false;
                msjeRespuesta = $"No se encontró la reserva con el código {bookingCode}";
            }
            else
            {
                success = true;
                msjeRespuesta = reserva.ToString();
            }

            return new
            {
                Success = success,
                Message = msjeRespuesta
            };
        }


        static string GenerarCodigoAlfanumerico(int longitud)
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();

            return new string(Enumerable.Range(0, longitud)
                .Select(_ => caracteres[random.Next(caracteres.Length)])
                .ToArray());
        }

    }
}
