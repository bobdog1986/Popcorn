using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    public class MediaRequest : RequestWithId
    {
        public MediaRequest(string requestType, long mediaSessionId, int? requestId = null) : base(requestType, requestId)
        {
            MediaSessionId = mediaSessionId;
        }

        [DataMember(Name = "mediaSessionId")]
        public long MediaSessionId { get; set; }
    }
}
