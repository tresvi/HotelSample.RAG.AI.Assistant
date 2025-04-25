using Microsoft.SemanticKernel;
using System.ComponentModel;
using TextCopy;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class ClipboardPlugin
    {
        [KernelFunction, Description("Copia un texto al portapapeles del sistema operativo")]
        public Task<object> CopiarAlPortapapelesAsync(
            [Description("Texto que se desea copiar al portapapeles")] string texto)
        {
            try
            {
                ClipboardService.SetTextAsync(texto);
                return Task.FromResult<object>(
                    new { 
                        Success = true
                    });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(
                    new { 
                        Success = false, 
                        Respuesta = $"Error al copiar al portapapeles: {ex.Message}" 
                    });
            }
        }
    }
}