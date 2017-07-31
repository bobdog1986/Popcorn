using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]

    public class VolumeDataObject
    {
        [DataMember(Name = "level")]
        public double? Level { get; set; }

        [DataMember(Name = "muted")]
        public bool? Muted { get; set; }
    }
}