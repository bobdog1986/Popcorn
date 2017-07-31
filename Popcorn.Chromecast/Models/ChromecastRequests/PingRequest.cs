using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class PingRequest : Request
    {
        public PingRequest()
            : base("PING")
        {
        }
    }
}