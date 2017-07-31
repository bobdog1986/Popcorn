using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Popcorn.Chromecast.Models;

namespace Popcorn.Chromecast.Services
{
    public class ChromecastService
    {
        private static readonly Lazy<ChromecastService> _current = new Lazy<ChromecastService>(() => new ChromecastService());
        public static ChromecastService Current => _current.Value;

        public DeviceLocator DeviceLocator { get; }
        public ChromeCastClient ChromeCastClient { get; }
        public ChromeCast ConnectedChromecast { get; set; }

        public ChromecastService()
        {
            DeviceLocator = new DeviceLocator();
            ChromeCastClient = new ChromeCastClient();
        }

  
        public async Task ConnectToChromecast(ChromeCast chromecast)
        {
            ConnectedChromecast = chromecast;
            await ChromeCastClient.ConnectChromecast(chromecast.DeviceUri);
        }
        

        public async Task<ObservableCollection<ChromeCast>> StartLocatingDevices()
        {
            return await DeviceLocator.LocateDevicesAsync();
        }

        public async Task<ObservableCollection<ChromeCast>> StartLocatingDevices(string localIpAdress)
        {
            return await DeviceLocator.LocateDevicesAsync(localIpAdress);
        }
    }
}