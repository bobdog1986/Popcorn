using System.Threading.Tasks;
using Popcorn.Chromecast.Models;

namespace Popcorn.Chromecast.Services
{
    public interface IChromecastService
    {
        DeviceLocator DeviceLocator { get; }
        ChromeCastClient ChromeCastClient { get; }
        ChromeCast ConnectedChromecast { get; set; }
        void ConnectToChromecast(ChromeCast chromecast);
        void StopLocatingDevices();
        Task StartLocatingDevices();
        ChromecastService Current { get; }
    }
}