using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Shows
{
    public class ShowResponse
    {
        [DataMember(Name = "totalShows")]
        public int TotalShows { get; set; }

        [DataMember(Name = "shows")]
        public List<ShowJson> Shows { get; set; }
    }
}
