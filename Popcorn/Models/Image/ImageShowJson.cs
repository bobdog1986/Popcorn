using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.Image
{
    public class ImageShowJson
    {
        [DataMember(Name = "poster")]
        public string Poster { get; set; }

        [DataMember(Name = "fanart")]
        public string Fanart { get; set; }

        [DataMember(Name = "banner")]
        public string Banner { get; set; }
    }
}
