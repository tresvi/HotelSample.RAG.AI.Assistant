using System.Text.Json.Serialization;

namespace HotelSample.RAG.AI.Assistant.Models
{
    public class Neo4jRequest
    {
        [JsonPropertyName("statements")]
        public List<Neo4jStatement> Statements { get; set; } = new();

        public Neo4jRequest(string cypherQuery)
        {
            Statements = new List<Neo4jStatement>();
            Statements.Add(new Neo4jStatement(cypherQuery));
        }
    }

    public class Neo4jStatement
    {
        [JsonPropertyName("statement")]
        public string Statement { get; set; } = string.Empty;

        public Neo4jStatement(string cypherQuery)
        {
            Statement = cypherQuery;
        }
    }
}
