using Newtonsoft.Json;
using Popcorn.Chromecast.JsonConverters;
using Popcorn.Chromecast.Models.Metadata;

namespace Popcorn.Chromecast.Models.MediaStatus
{
    public class Media
    {
        public string contentId { get; set; }
        public string contentType { get; set; }
        public string streamType { get; set; }
        public double duration { get; set; }
        [JsonConverter(typeof(MetadataTypeConverter))]
        public IMetadata metadata { get; set; }
        public object customData { get; set; }
    }
}