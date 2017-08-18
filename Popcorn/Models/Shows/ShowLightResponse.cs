using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Deserializers;

namespace Popcorn.Models.Shows
{
    public class ShowLightResponse
    {
        [DeserializeAs(Name = "totalShows")]
        public int TotalShows { get; set; }

        [DeserializeAs(Name = "shows")]
        public List<ShowLightJson> Shows { get; set; }
    }
}
