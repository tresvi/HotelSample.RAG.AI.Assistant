using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    internal class ArchivosEscrituraPlugin
    {
        private const string WRITE_ROOT_DIR = @"C:\Temp\Agente\";

        [KernelFunction, Description("Escribe archivos en el Filesystem local")]
        public async Task<object> GuardarArchivo(
            [Description("Nombre del archivo a guardar")] string fileName,
            [Description("Contenido a guardar dentro del archivo")] string contenido
        )
        {
            try
            {
                if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".txt";
                }

                string fullPath = Path.Combine(WRITE_ROOT_DIR, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); // Asegura que exista el directorio

                await File.WriteAllTextAsync(fullPath, contenido);

                return Task.FromResult<object>(
                    new
                    {
                        Success = true,
                        Response = $"Archivo guardado en {fullPath}",
                        Path = fullPath
                    });
            }
            catch (IOException ex)
            {
                return Task.FromResult<object>(
                    new
                    {
                        Success = false,
                        Response = $"Error al intentar guardar el archivo. {ex.Message}"
                    });
            }
        }
    }
}
