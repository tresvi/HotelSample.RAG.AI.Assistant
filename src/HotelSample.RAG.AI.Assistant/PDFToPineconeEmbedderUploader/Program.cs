using Azure;
using Azure.AI.OpenAI;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
using System.Text.Json;

namespace PDFToPineconeEmbedderUploader
{

    public class ParagraphVector
    {
        public string Content { get; set; }
        public List<float> Vectors { get; set; }
    }

    public class PineconeVector
    {
        public string Id { get; set; }
        public List<float> Values { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }


    internal class Program
    {
        const string AZURE_OPENAI_KEY = "14a18e551aeb4aaea185b475bb968226";
        const string AZURE_OPENAI_URL = "https://prueba1234.openai.azure.com/";
        const string AZURE_OPENAI_EMBEDD_IMPL = "embedding";

        static async Task Main(string[] args)
        {
            string pdfPath = "hotel-valle-del-volcan.pdf"; // Path to the PDF file
            string text = ExtractTextFromPdf(pdfPath);
            List<string> paragraphs = SplitIntoParagraphs(text);
            List<ParagraphVector> paragraphVectors = new List<ParagraphVector>();

            foreach (var paragraph in paragraphs)
            {
                var vectors = await GetEmbeddings(paragraph);
                paragraphVectors.Add(new ParagraphVector { Content = paragraph, Vectors = vectors.ToList() });
            }

            await UploadVectorsToPinecone(paragraphVectors);
        }


        static string ExtractTextFromPdf(string path)
        {
            StringBuilder text = new StringBuilder();

            using (PdfReader pdfReader = new PdfReader($@"..\..\..\{path}"))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.Append(pageText);
                }
            }

            return text.ToString();
        }


        static List<string> SplitIntoParagraphs(string text)
        {
            return new List<string>(text.Split(new[] { "\r\n\r\n", "\n \n" }, StringSplitOptions.None).Where(x => x.Trim() != string.Empty));
        }


        static async Task<IEnumerable<float>> GetEmbeddings(string text)
        {
            OpenAIClient client = new OpenAIClient(
                new Uri(AZURE_OPENAI_URL),
                new AzureKeyCredential(AZURE_OPENAI_KEY)
             );


            var userQuestionEmbedding = await client.GetEmbeddingsAsync(new EmbeddingsOptions("embedding", [text]));

            return userQuestionEmbedding.Value.Data[0].Embedding.ToArray();
        }


        const string PINECONE_URL = "https://rag-assistant-test-dnq0bbo.svc.aped-4627-b74a.pinecone.io";
        const string PINECONE_API_KEY = "4248cc68-5f9a-43c5-9824-d93770850ec3";

        static async Task UploadVectorsToPinecone(List<ParagraphVector> paragraphVectors)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiKey = PINECONE_API_KEY;
                string endpoint = $"{PINECONE_URL}/vectors/upsert";
                client.DefaultRequestHeaders.Add("Api-Key", apiKey);

                var vectors = new List<PineconeVector>();
                int id = 1;

                foreach (var paragraphVector in paragraphVectors)
                {
                    vectors.Add(new PineconeVector
                    {
                        Id = "vec" + id++,
                        Values = paragraphVector.Vectors,
                        Metadata = new Dictionary<string, string> { { "content", paragraphVector.Content } }
                    });
                }

                var payload = new { vectors = vectors, @namespace = "ns1" };

                string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{vectors.Count} Vectors uploaded successfully.");
                }
                else
                {
                    Console.WriteLine("Error uploading vectors: " + response.ReasonPhrase);
                }
            }
        }


    }
}
