#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using HotelSample.RAG.AI.Assistant.PlugIns;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Tiktoken;


namespace HotelSample.RAG.AI.Assistant
{
    internal class Program
    {
        const string AZURE_OPENAI_KEY = "14a18e551aeb4aaea185b475bb968226";
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";
        const string AZURE_OPENAI_IMPL = "gpt-4o-mini";//"g-35";
        const string AZURE_OPENAI_EMBEDD_IMPL = "embedding";

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

                builder.Plugins.AddFromType<ReservacionesPlugin>("Reservaciones");
                builder.Plugins.AddFromType<PineconeQueryPlugin>("PineconeQuery");
                //builder.Plugins.AddFromType<WebSearchDuckDuckGOPlugin>("WebSearchDuckDuckGo");
                builder.Plugins.AddFromType<WebSearchSerpApiPlugin>("WebSearchSerpAPI");
                builder.Plugins.AddFromType<ClipboardPlugin>("Clipboard");
                var kernel = builder.Build();

                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

                var history = new ChatHistory();

                string prompt = File.ReadAllText(@$"..\..\..\Prompts\{PROMPT}");
                prompt += $"\nTené en cuenta que la fecha de hoy es {DateTime.Now}.";
                history.AddSystemMessage(prompt);

                var settings = new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.4f
                };

                Console.WriteLine($"*******Hotel valle del volcan, bienvenido - {DateTime.Now:hh:mm:ss}*******");

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string userInput = Console.ReadLine();

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
