using Newtonsoft.Json;

namespace Popcorn.Chromecast.Models.ChromecastStatus
{
    public class ChromecastStatusResponse
    {
        public int requestId { get; set; }
        [JsonProperty("status")]
        public ChromecastStatus ChromecastStatus { get; set; }
        public string type { get; set; }
    }
}