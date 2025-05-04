using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Plugins
{
    public class WebSearchDuckDuckGOPlugin
    {
        private readonly HttpClient _httpClient = new();

        [KernelFunction, Description("Busca una respuesta rápida en Internet sobre el tema indicado.")]
        public async Task<string> BuscarEnInternetAsync(
            [Description("Consulta a buscar en Internet")] string consulta)
        {
            try
            {
                // DuckDuckGo Instant Answer API (sin key)
                string url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(consulta)}&format=json&no_redirect=1&no_html=1";

                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                var root = doc.RootElement;
                if (root.TryGetProperty("AbstractText", out JsonElement abstractText) &&
                    !string.IsNullOrWhiteSpace(abstractText.GetString()))
                {
                    return abstractText.GetString()!;
                }

                if (root.TryGetProperty("RelatedTopics", out JsonElement relatedTopics) &&
                    relatedTopics.ValueKind == JsonValueKind.Array &&
                    relatedTopics.GetArrayLength() > 0)
                {
                    var first = relatedTopics[0];
                    if (first.TryGetProperty("Text", out JsonElement text))
                    {
                        return text.GetString()!;
                    }
                }

                return "No se encontró una respuesta útil en la web.";
            }
            catch (Exception ex)
            {
                return $"Error al buscar en la web: {ex.Message}";
            }
        }
    }
}
