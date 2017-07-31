using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class StopApplicationRequest : RequestWithId
    {
        public StopApplicationRequest(string sessionId, int? requestId = null)
            : base("STOP", requestId)
        {
            SessionId = sessionId;
        }

        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }
    }
}