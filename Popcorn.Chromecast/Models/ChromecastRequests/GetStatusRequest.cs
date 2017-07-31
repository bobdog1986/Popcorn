using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class GetStatusRequest : RequestWithId
    {
        public GetStatusRequest(int? requestId = null)
            : base("GET_STATUS", requestId)
        {
        }
    }
}