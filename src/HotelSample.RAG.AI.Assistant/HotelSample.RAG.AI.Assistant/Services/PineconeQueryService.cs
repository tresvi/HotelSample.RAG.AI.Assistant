#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using HotelSample.RAG.AI.Assistant.Models;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Services
{
    public class PineconeQueryService
    {
        private readonly HttpClient _httpClient;
        private readonly ITextEmbeddingGenerationService _embeddingService;

        public PineconeQueryService(
            ITextEmbeddingGenerationService embeddingService,
            HttpClient httpClient)
        {
            _embeddingService = embeddingService;
            _httpClient = httpClient;
        }

        public async Task<string> ConsultarComodidadesAsync(string pregunta)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(pregunta);
            var payload = new
            {
                @namespace = "ns1",
                vector = embedding.ToArray(),
                topK = 3,
                includeValues = true,
                includeMetadata = true
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var response = await _httpClient.PostAsync("", new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PineconeResult>(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var contenido = string.Join(".\n ", result?.Matches?.Select(m => m.Metadata["content"]) ?? []);
            //return $"Basate en el siguiente contenido para responder sobre instalaciones:\n\n{contenido}\n\nPregunta: {pregunta}";
            string message = $"Responde si es posible, la pregunta: \"{pregunta}\" \n\n basandote en el " +
                $"siguiente contenido:\n\n {contenido}";

            return message;
        }
    }
}
