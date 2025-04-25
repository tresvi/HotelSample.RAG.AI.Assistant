using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class AbrirArchivoConNotepadPlugin
    {
        private const string NOTEPADPP_PATH = @"C:\Program Files\Notepad++\notepad++.exe";
        private const string FILE_ROOT_DIR = @"C:\Temp\Agente\";

        [KernelFunction, Description("Abre un archivo con Notepad++")]
        public Task<object> AbrirArchivoConNotepad(
            //[Description("Nombre del archivo que se abrirá con Notepad++")] string fileName)
            [Description("Path completo del archivo que se abrirá con Notepad++ cuando el usuario lo especifique que quiere verlo en notepad o en un editor")] string fullPath)
        {
            try
            {
                //string fullPath = Path.Combine(FILE_ROOT_DIR, fileName);

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult<object>(new
                    {
                        Success = false,
                        Response = "El archivo no existe."
                    });
                }

                if (!File.Exists(NOTEPADPP_PATH))
                {
                    return Task.FromResult<object>(new
                    {
                        Success = false,
                        Response = "No se encontró Notepad++ en la ruta especificada."
                    });
                }

                Process.Start(NOTEPADPP_PATH, fullPath);

                return Task.FromResult<object>(new
                {
                    Success = true,
                    //Response = $"Archivo '{fileName}' abierto con Notepad++."
                    Response = $"Archivo '{Path.GetFileName(fullPath)}' abierto con Notepad++."
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new
                {
                    Success = false,
                    Response = $"Error al intentar abrir el archivo: {ex.Message}"
                });
            }
        }
    }
}