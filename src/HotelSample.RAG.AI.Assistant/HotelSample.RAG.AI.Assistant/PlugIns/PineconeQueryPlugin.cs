#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using HotelSample.RAG.AI.Assistant.Models;
using HotelSample.RAG.AI.Assistant.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;


namespace HotelSample.RAG.AI.Assistant.Plugins
{
    internal class PineconeQueryPlugin
    {
        const string PINECONE_URL = "https://rag-assistant-test-dnq0bbo.svc.aped-4627-b74a.pinecone.io/query";
        //const string PINECONE_URL = "https://infobna-dnq0bbo.svc.aped-4627-b74a.pinecone.io/indexes/infobna/query";
        const string PINECONE_API_KEY = "4248cc68-5f9a-43c5-9824-d93770850ec3";
        const int TOPK = 6;
        const float SCORE_MIN = 0.82f;


        private readonly ITextEmbeddingGenerationService _embeddingService;
        public PineconeQueryPlugin(ITextEmbeddingGenerationService embeddingService)
        {
            _embeddingService = embeddingService;
        }

        [KernelFunction("comodidades_pinecone_query")]
        [Description("Busca información general sobre instalaciones y servicios del hotel. Esta función se ejecuta cuando no tengas información como para responder al cliente.")]
        public async Task<object> ConsultarComodidades(
        [Description("La consulta que realiza el usuario acerca de instalaciones, servicios y comodidades.")] string pregunta)
        {
            Debug.WriteLine($"********Pregunta: {pregunta}");

            var vectors = await _embeddingService.GenerateEmbeddingAsync(pregunta!);
            PineconeResult pineconeResult = await QueryPinecone(vectors.ToArray(), "");

            Debug.WriteLine("**********RESPUESTA CRUDA***********");
            foreach (Match match in pineconeResult.Matches)
            {
                Debug.WriteLine($"id:{match.Id}\tScore:{match.Score}\tMetadata:{match.Metadata.Values}");
            }

            pineconeResult.Matches = pineconeResult.Matches.Where(x => x.Score > SCORE_MIN);

            Debug.WriteLine("********RESPUESTA FILTRADA**********");
            foreach (Match match in pineconeResult.Matches)
            {
                Debug.WriteLine($"id:{match.Id}\tScore:{match.Score}\tMetadata:{match.Metadata.Values}");
            }

            int respuestasAceptables = pineconeResult.Matches.Count(x => x.Score > SCORE_MIN);
            if (respuestasAceptables == 0)
            {
                return new{Success = false, Respuesta = ""};
            }

            string message = "Si la pregunta del usuario es sobre instalaciones y caracteristicas del hotel " +
                "debes basarte en el siguiente contenido para responder ese tipo de consultas\n\n" +
                string.Join(".\n ", pineconeResult.Matches.Select(x => x.Metadata["content"])) +
                "\n Pregunta del usuario: " + pregunta;

            return new{ Success = true, Respuesta = message};
        }


        static async Task<PineconeResult> QueryPinecone(float[] vectors, string keyword)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiKey = PINECONE_API_KEY;
                string endpoint = PINECONE_URL;
                client.DefaultRequestHeaders.Add("Api-Key", apiKey);

                //https://docs.pinecone.io/guides/data/filter-with-metadata
                var filter = new Dictionary<string, Dictionary<string, string>>();

                filter.Add("content", new Dictionary<string, string>()
                {
                    { "$in", keyword }
                });

                var payload = new
                {
                    @namespace = "ns1",
                    vector = vectors,
                    topK = TOPK,
                    includeValues = true,
                    includeMetadata = true
                };

                string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                PineconeResult result = await response.Content.ReadFromJsonAsync<PineconeResult>(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return result;
            }
        }

        /*
        public ComodidadesPineconeQueryPlugin(
            ITextEmbeddingGenerationService embeddingService,
            HttpClient httpClient)
        {
            _embeddingService = embeddingService;
            _httpClient = httpClient;
        }

        [KernelFunction("comodidades_pinecone_query")]
        [Description("Busca información general sobre instalaciones y servicios del hotel. Esta función se ejecuta cuando no tengas información como para responder al cliente.")]
        public async Task<object> ConsultarComodidades(
            [Description("La consulta que realiza el usuario acerca de instalaciones, servicios y comodidades.")] string pregunta)
        {
            Debug.WriteLine($"********Pregunta: {pregunta}");

            var vectors = await _embeddingService.GenerateEmbeddingAsync(pregunta!);
            var pineconeResult = await QueryPineconeAsync(vectors.ToArray(), "");

            string message = "Si la pregunta del usuario es sobre instalaciones y características del hotel, " +
                "debes basarte en el siguiente contenido para responder ese tipo de consultas:\n\n" +
                string.Join(".\n ", pineconeResult.Matches.Select(x => x.Metadata["content"])) +
                "\n Pregunta del usuario: " + pregunta;

            return new
            {
                Success = true,
                Respuesta = "Esta es la respuesta basada en Pinecone."
            };
        }
        */

        /*
        private readonly PineconeQueryService _pineconeQueryService;

        public PineconeQueryPlugin(PineconeQueryService service)
        {
            _pineconeQueryService = service;
        }

        [KernelFunction("comodidades_pinecone_query")]
        [Description("Busca información general sobre instalaciones y servicios del hotel.")]
        public async Task<object> ConsultarComodidades(
            [Description("Pregunta del usuario sobre instalaciones, servicios y comodidades del hotel")] string pregunta)
        {
            var resultado = await _pineconeQueryService.ConsultarComodidadesAsync(pregunta);
            return new
            {
                Success = true,
                Respuesta = resultado
            };
        }
        */
    }
}
