using System.Runtime.Serialization;

namespace Popcorn.Chromecast.Models.ChromecastRequests
{
    [DataContract]
    public class CloseRequest : Request
    {
        public CloseRequest()
            : base("CLOSE")
        {
        }
    }
}