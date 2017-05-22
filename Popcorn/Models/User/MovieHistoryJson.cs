using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class MovieHistoryJson
    {
        [DeserializeAs(Name = "ImdbId")]
        public string ImdbId { get; set; }

        [DeserializeAs(Name = "Seen")]
        public bool Seen { get; set; }

        [DeserializeAs(Name = "Favorite")]
        public bool Favorite { get; set; }
    }
}
