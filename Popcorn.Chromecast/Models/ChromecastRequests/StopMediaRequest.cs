using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class StopMediaRequest : RequestWithId
    {
        public StopMediaRequest(long mediaSessionId, int? requestId = null)
            : base("STOP", requestId)
        {
            MediaSessionId = mediaSessionId;
        }

        [DataMember(Name = "mediaSessionId")]
        public long MediaSessionId { get; set; }
    }
}
