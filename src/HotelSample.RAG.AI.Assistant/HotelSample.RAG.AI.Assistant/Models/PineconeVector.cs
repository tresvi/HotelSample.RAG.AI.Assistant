using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Models
{
    internal class PineconeVector
    {
        public string Id { get; set; }
        public IEnumerable<float> Values { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
