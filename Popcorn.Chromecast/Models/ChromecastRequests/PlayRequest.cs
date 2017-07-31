using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class PlayRequest : MediaRequest
    {
        public PlayRequest(long mediaSessionId, int? requestId = null)
            : base("PLAY", mediaSessionId, requestId)
        {
        }
    }
}