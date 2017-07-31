using System.Collections.Generic;
using Newtonsoft.Json;
using Popcorn.Chromecast.JsonConverters;
using Popcorn.Chromecast.Models.Enums;
using Popcorn.Chromecast.Models.MediaStatus;

namespace Popcorn.Chromecast.Models.Metadata
{
    public interface IMetadata
    {
        List<ChromecastImage> images { get; set; }
        [JsonConverter(typeof(MetadataTypeEnumConverter))]
        MetadataTypeEnum metadataType { get; set; }
        string title { get; set; }
    }
}
