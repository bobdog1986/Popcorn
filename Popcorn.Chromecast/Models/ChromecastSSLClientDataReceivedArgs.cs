using System;
using Extensions.Api.CastChannel;

namespace Popcorn.Chromecast.Models
{
    public class ChromecastSSLClientDataReceivedArgs : EventArgs
    {
        public ChromecastSSLClientDataReceivedArgs(CastMessage message)
        {
            Message = message;
        }
        public CastMessage Message { get; set; }
    }
}