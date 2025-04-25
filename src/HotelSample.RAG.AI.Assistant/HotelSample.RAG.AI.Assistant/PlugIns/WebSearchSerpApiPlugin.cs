using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class WebSearchSerpApiPlugin
    {
        private const string SERP_API_KEY = "8237ecf4c4060a4bc472476ab2447cea0ad35d6683484d273a72975ef8c32fda";
        private const string ENDPOINT = "https://serpapi.com/search";

        private static readonly HttpClient _httpClient = new HttpClient();

        [KernelFunction, Description("Busca en internet utilizando SerpAPI. Esta funcion se invoca cada vez que se requiera hacer una busqueda en internet")]
        public async Task<string> BuscarEnInternetAsync(
            [Description("La consulta a buscar en internet")] string consulta)
        {
            Console.WriteLine("...buscando en internet...");
            var url = $"{ENDPOINT}?q={Uri.EscapeDataString(consulta)}&api_key={SERP_API_KEY}&hl=es&gl=ar";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            // Extrae los primeros resultados
            if (json.RootElement.TryGetProperty("organic_results", out var results))
            {
                var resumen = "";

                foreach (var result in results.EnumerateArray().Take(3))
                {
                    var title = result.GetProperty("title").GetString();
                    var snippet = result.GetProperty("snippet").GetString();
                    var link = result.GetProperty("link").GetString();

                    resumen += $"Título: {title}\nResumen: {snippet}\nEnlace: {link}\n\n";
                }

                return string.IsNullOrWhiteSpace(resumen) ? "No se encontraron resultados." : resumen;
            }

            return "No se encontraron resultados.";
        }
    }
}
