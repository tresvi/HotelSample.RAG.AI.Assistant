#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using HotelSample.RAG.AI.Assistant.PlugIns;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace HotelSample.RAG.AI.Assistant
{
    internal class Program2
    {
        const string AZURE_OPENAI_KEY = "14a18e551aeb4aaea185b475bb968226";
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";
        async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: "chat",
                endpoint: AZURE_OPENAI_URL,
                apiKey: AZURE_OPENAI_KEY);

            builder.Plugins.AddFromType<UserPlugin>("UserInfo");

            var kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            ChatHistory history = new ChatHistory();

            history.AddUserMessage(@"Hola,mi nombre es Lautaro Carro, naci el 2 de julio de 1998 y mi email es lautaroecarro@gmail.com");

            var result = await chatService.GetChatMessageContentAsync(
                history,
                executionSettings: new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                },
               kernel: kernel);

            Console.WriteLine(result.Items[0].ToString());

            // Add the message from the agent to the chat history
            history.AddAssistantMessage(result.Items[0].ToString());

        }
    }
}
