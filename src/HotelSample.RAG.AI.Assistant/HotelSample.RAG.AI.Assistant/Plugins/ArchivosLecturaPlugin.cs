using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    
    internal class ArchivosLecturaPlugin
    {
        private const string READ_ROOT_DIR = @"C:\Temp\Agente\";
        private const string READ_PROGRAMAS_DIR = @"C:\Temp\Agente\Programas\";

        [KernelFunction, Description("Lee archivos y archivos que contienen programas del filesystem local.")]
        public async Task<object> LeerArchivo(
            [Description("Nombre del archivo que se abrirá")]  string fileName,
            [Description("Si se solicita leer un programa, este valor va en true")] bool esPrograma,
            [Description("Vale true si el usuario solo pidio que se muestre el archivo," +
                " si el usuario pretende analizarlo u otra accion sobre el contenido, vale false")] bool soloMostrar
            )
        {
            try
            {
                string fullPath = esPrograma ? READ_PROGRAMAS_DIR + fileName : READ_ROOT_DIR + fileName;
                string content = await File.ReadAllTextAsync(fullPath);

                if (soloMostrar)
                {
                    Console.WriteLine(content);
                    return Task.FromResult<object>(new { Success = true, Path = fullPath });
                }
                else
                {
                    return Task.FromResult<object>(new { Success = true, Response = content, Path = fullPath });
                }
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult<object>(
                new
                {
                    Success = false,
                    Response = "Archivo no encontrado"
                });
            }
            catch (IOException)
            {
                return Task.FromResult<object>(
                new
                {
                    Success = false,
                    Response = "Error al intentar abrir el archivo"
                });
            }
        }

    }
}
