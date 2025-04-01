#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using HotelSample.RAG.AI.Assistant.PlugIns;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace HotelSample.RAG.AI.Assistant
{
    internal class Program
    {
        const string AZURE_OPENAI_KEY = "14a18e551aeb4aaea185b475bb968226";
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";
        const string AZURE_OPENAI_IMPL = "gpt-4o-mini";//"g-35";
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
                Console.WriteLine("*******Hotel valle del volcan, bienvenido*******");

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string userInput = Console.ReadLine();

                    history.AddUserMessage(userInput);
                    Console.ForegroundColor = ConsoleColor.Green;
                    string response = "";
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
        // EnvironmentVariableTarget history =

    }
}
