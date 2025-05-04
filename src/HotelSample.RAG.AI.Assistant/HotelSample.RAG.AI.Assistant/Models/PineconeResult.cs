using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotelSample.RAG.AI.Assistant.Models
{
    internal class PineconeResult
    {
        public List<object> Results { get; set; }
        public IEnumerable<Match> Matches { get; set; }
    }
}
