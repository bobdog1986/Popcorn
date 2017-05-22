using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Deserializers;

namespace Popcorn.Models.Genres
{
    public class GenreJson
    {
        [DeserializeAs(Name = "EnglishName")]
        public string EnglishName { get; set; }

        [DeserializeAs(Name = "Name")]
        public string Name { get; set; }
    }

    public class GenreResponse
    {
        [DeserializeAs(Name = "genres")]
        public List<GenreJson> Genres { get; set; }
    }
}
