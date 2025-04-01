using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Models
{
    internal class Match
    {
        public string Id { get; set; }
        public float Score { get; set; }
        public List<float> Values { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
