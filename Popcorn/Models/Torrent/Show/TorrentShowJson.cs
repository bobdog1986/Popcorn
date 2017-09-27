using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Deserializers;

namespace Popcorn.Models.Torrent.Show
{
    public class TorrentShowJson
    {
        [DataMember(Name = "provider")]
        public string Provider { get; set; }

        [DataMember(Name = "peers")]
        public int? Peers { get; set; }

        [DataMember(Name = "seeds")]
        public int? Seeds { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
