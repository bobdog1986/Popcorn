using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Deserializers;

namespace Popcorn.Models.Torrent.Show
{
    public class TorrentShowJson
    {
        [DeserializeAs(Name = "provider")]
        public string Provider { get; set; }

        [DeserializeAs(Name = "peers")]
        public int? Peers { get; set; }

        [DeserializeAs(Name = "seeds")]
        public int? Seeds { get; set; }

        [DeserializeAs(Name = "url")]
        public string Url { get; set; }
    }
}
