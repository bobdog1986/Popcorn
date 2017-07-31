using System;
using System.Threading.Tasks;
using Extensions.Api.CastChannel;
using Popcorn.Chromecast.Models;

namespace Popcorn.Chromecast.Channels
{
    public interface IChromecastChannel
    {
        string Namespace { get; }
        event EventHandler<ChromecastSSLClientDataReceivedArgs> MessageReceived;
        Task Write(CastMessage message, bool includeNameSpace = true);
        void OnMessageReceived(ChromecastSSLClientDataReceivedArgs e);
    }
}