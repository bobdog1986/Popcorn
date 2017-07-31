using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class PauseRequest : MediaRequest
    {
        public PauseRequest(long mediaSessionId, int? requestId = null)
            : base("PAUSE", mediaSessionId, requestId)
        {
        }
    }
}