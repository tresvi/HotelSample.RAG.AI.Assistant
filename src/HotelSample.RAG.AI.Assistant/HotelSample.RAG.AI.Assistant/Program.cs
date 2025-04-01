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
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";//"https://prueba1234.openai.azure.com/";

        public static async Task Main()
        {
            try
            {

                var builder = Kernel.CreateBuilder();

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: "g-35",
                    endpoint: AZURE_OPENAI_URL,
                    apiKey: AZURE_OPENAI_KEY
                );

                builder.Plugins.AddFromType<HotelPlugin>("Hotel");
                var kernel = builder.Build();

                var history = new ChatHistory();
                history.AddSystemMessage(@$"Sos el asistente de AI de un Hotel de Buenos Aires, Argentina.
                    Tu tarea es consultar a los usuarios si quieren realizar una reserva.

    Si el usuario desea realizar una reserva, tenes que preguntarle al usuario como se llama, cuantas personas se van a 
    alojar, desde que día se van a alogar, hasta cuando y si desea el plan Basic que cuesta 10 dolares o el Standard que 
    cuasta 30 dolares o el plan Premium que cuesta 50 dolares.

    Todos estos datos los tenes que preguntar uno por cada pregunta.
    No hagas más de una pregunta a la vez.
    Una vez que tengas todos estos datos, vas a pedir la confirmación del usuario,

    Tené en cuenta que la fecha de hoy es {DateTime.Now}.");

                var settings = new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.1f
                };

                var chatService = kernel.GetRequiredService<IChatCompletionService>();

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
