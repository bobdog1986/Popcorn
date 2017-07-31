using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class PongRequest : Request
    {
        public PongRequest()
            : base("PONG")
        {
        }
    }
}