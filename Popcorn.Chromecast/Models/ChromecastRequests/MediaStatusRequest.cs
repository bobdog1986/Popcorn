using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class MediaStatusRequest : RequestWithId

    {
        public MediaStatusRequest(int? requestId = null) : base("GET_STATUS", requestId)
        {
        }
    }
}
