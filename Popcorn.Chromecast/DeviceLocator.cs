using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Rssdp;
using Rssdp.Infrastructure;
using Popcorn.Chromecast.Annotations;
using Popcorn.Chromecast.Models;
using Tmds.MDns;
using Zeroconf;
using System.Linq;

namespace Popcorn.Chromecast
{
    public class DeviceLocator : INotifyPropertyChanged
    {
        public ObservableCollection<ChromeCast> DiscoveredDevices { get; set; }

        public DeviceLocator()
        {
            DiscoveredDevices = new ObservableCollection<ChromeCast>();
        }

        public async Task<ObservableCollection<ChromeCast>> LocateDevicesAsync()
        {
            return await LocateDevicesAsync(new SsdpDeviceLocator());
        }

        public async Task<ObservableCollection<ChromeCast>> LocateDevicesAsync(string localIpAdress)
        {
            return await LocateDevicesAsync(new SsdpDeviceLocator(new SsdpCommunicationsServer(new SocketFactory(localIpAdress))));
        }

        private async Task<ObservableCollection<ChromeCast>> LocateDevicesAsync(SsdpDeviceLocator deviceLocator)
        {
            var responses = await ZeroconfResolver.ResolveAsync("_googlecast._tcp.local.");
            foreach (var resp in responses)
            {
                Uri uri;
                if (Uri.TryCreate("https://" + resp.IPAddress, UriKind.Absolute, out uri))
                {
                    var chromecast = new ChromeCast
                    {
                        DeviceUri = uri,
                        FriendlyName = resp.Services.Select(a => a.Value.Properties.Select(b => b["fn"])).FirstOrDefault().FirstOrDefault()
                    };
                    DiscoveredDevices.Add(chromecast);
                }
            }

            return DiscoveredDevices;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}