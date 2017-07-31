using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class ConnectRequest : Request
    {
        public ConnectRequest()
            : base("CONNECT")
        {
        }
    }
}