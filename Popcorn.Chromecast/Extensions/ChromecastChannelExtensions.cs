using System.Collections.Generic;
using Popcorn.Chromecast.Channels;

namespace Popcorn.Chromecast.Extensions
{
    public static class ChromecastClientExtensions
    {
        public static void MakeSureChannelExist(this ChromeCastClient client, IEnumerable<IChromecastChannel> channels)
        {
            foreach (var channel in channels)
            {
                if (!client.Channels.Exists(x => x.Namespace == channel.Namespace))
                {
                    client.Channels.Add(channel);
                }
            }
        }

        public static void MakeSureChannelExist(this ChromeCastClient client, IChromecastChannel channel)
        {
            client.MakeSureChannelExist(new List<IChromecastChannel> {channel});
        }
    }
}
