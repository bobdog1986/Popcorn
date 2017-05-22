using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class LanguageJson
    {
        [DeserializeAs(Name = "Culture")]
        public string Culture { get; set; }

        public string Name { get; set; }
    }
}
