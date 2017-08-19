using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class MovieHistory
    {
        public string ImdbId { get; set; }

        public bool Seen { get; set; }

        public bool Favorite { get; set; }
    }
}
