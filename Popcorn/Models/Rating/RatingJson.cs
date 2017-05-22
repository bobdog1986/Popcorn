using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Rating
{
    public class RatingJson
    {
        [DeserializeAs(Name = "percentage")]
        public int? Percentage { get; set; }

        [DeserializeAs(Name = "watching")]
        public int? Watching { get; set; }

        [DeserializeAs(Name = "votes")]
        public int? Votes { get; set; }

        [DeserializeAs(Name = "loved")]
        public int? Loved { get; set; }

        [DeserializeAs(Name = "hated")]
        public int? Hated { get; set; }
    }
}
