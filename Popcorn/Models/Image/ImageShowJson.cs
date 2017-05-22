using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Image
{
    public class ImageShowJson
    {
        [DeserializeAs(Name = "poster")]
        public string Poster { get; set; }

        [DeserializeAs(Name = "fanart")]
        public string Fanart { get; set; }

        [DeserializeAs(Name = "banner")]
        public string Banner { get; set; }
    }
}
