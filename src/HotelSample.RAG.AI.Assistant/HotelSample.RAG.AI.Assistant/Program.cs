#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using HotelSample.RAG.AI.Assistant.Models;
using HotelSample.RAG.AI.Assistant.PlugIns;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Tiktoken;
using System.Diagnostics;


namespace HotelSample.RAG.AI.Assistant
{
    internal class Program
    {
        const string AZURE_OPENAI_KEY = "14a18e551aeb4aaea185b475bb968226";
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";
        const string AZURE_OPENAI_IMPL = "gpt-4o-mini";//"g-35";
        const string AZURE_OPENAI_EMBEDD_IMPL = "embedding";

        const string PINECONE_URL = "https://rag-assistant-test-dnq0bbo.svc.aped-4627-b74a.pinecone.io/query";
        //const string PINECONE_URL = "https://infobna-dnq0bbo.svc.aped-4627-b74a.pinecone.io/indexes/infobna/query";
        const string PINECONE_API_KEY = "4248cc68-5f9a-43c5-9824-d93770850ec3";

        const string PROMPT = "Prompt_V2.txt";


        public static async Task Main()
        {
            try
            {
                var builder = Kernel.CreateBuilder();

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: AZURE_OPENAI_IMPL,
                    endpoint: AZURE_OPENAI_URL,
                    apiKey: AZURE_OPENAI_KEY
                );
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    AZURE_OPENAI_EMBEDD_IMPL,
                    AZURE_OPENAI_URL, 
                    AZURE_OPENAI_KEY
                );

                builder.Plugins.AddFromType<HotelPlugin>("Hotel");
                var kernel = builder.Build();

                var history = new ChatHistory();
                string prompt = File.ReadAllText(@$"..\..\..\Prompts\{PROMPT}");

                prompt += $"\nTené en cuenta que la fecha de hoy es {DateTime.Now}.";
                history.AddSystemMessage(prompt);

                var settings = new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.4f
                };

                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

                Console.WriteLine($"*******Hotel valle del volcan, bienvenido - {DateTime.Now:hh:mm:ss}*******");

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string userInput = Console.ReadLine();

                    /**********************PINECONE*******************************/
                    var vectors = await embeddingService.GenerateEmbeddingAsync(userInput!);

                    var pineconeResult = await QueryPinecone(vectors.ToArray(), "");

                    string message = "Si la pregunta del usuario es sobre instalaciones y caracteristicas del hotel " +
                        "debes basarte en el siguiente contenido para responder ese tipo de consultas\n\n" +
                        string.Join(".\n ", pineconeResult.Matches.Select(x => x.Metadata["content"])) +
                        "\n Pregunta del usuario: " + userInput;

                    Olvidar(ref history);
                    history.AddUserMessage(message!);
                    Debug.WriteLine(history.ToString());
                    Debug.WriteLine($"*******Tokens de la pregunta: {CountTokens(message)}");
                    /*************************************************************/

                    history.AddUserMessage(userInput);
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    string response = "";

                    Console.Write(DateTime.Now.ToString("hh:mm:ss - "));
                    await foreach (var item in chatService.GetStreamingChatMessageContentsAsync(history, settings, kernel))
                    {
                        response = response + item.ToString();
                        Console.Write(item.ToString());
                    }

                    Console.WriteLine();
                    history.AddAssistantMessage(response);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR:" + ex.ToString());
            }
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
                    topK = 3,
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


        private static Tiktoken.Encoder _encoder = ModelToEncoder.For("gpt-35-turbo"); // or explicitly using new Encoder(new O200KBase())
        public static int CountTokens(string textToCuont)
        {
            return _encoder.CountTokens(textToCuont); // Cuenta la cantidad de tokens de la frase
            //Devuelve un array de int con la frase tokenizada:    var tokens = _encoder.Encode(textToCuont); 
            //Recompone el string original a partir de un array de tokens:     string text = _encoder.Decode(tokens); 
            //Devuelve un array con los tokens tomados para esta frase:   stringTokens  = _encoder.Explore(textToCuont);
        }


        static void Olvidar(ref ChatHistory lista)
        {
            if (lista.Count <= 5)
            {
                //Console.WriteLine("La lista tiene 5 o menos elementos. No se elimina nada.");
                return;
            }

            lista.RemoveRange(2, lista.Count - 5);
        }
    }
}
