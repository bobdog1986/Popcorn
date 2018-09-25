using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCast;
using GoogleCast.Models.Media;
using Popcorn.Models.Chromecast;

namespace Popcorn.Services.Chromecast
{
    public interface IChromecastService
    {
        Task<IEnumerable<IReceiver>> FindReceiversAsync();
        Task LoadAsync(MediaInformation media, (bool hasSubtitle, int trackId) subtitle);
        Task<bool> ConnectAsync(IReceiver receiver);
        bool IsStopped { get; }
        bool IsMuted { get; }
        Task PauseAsync();
        Task PlayAsync();
        Task StopAsync();
        Task SeekAsync(double seconds);
        Task SetVolumeAsync(float volume);
        Task SetIsMutedAsync();
        Task<MediaStatus> GetStatus();
        event EventHandler<MediaStatusEventArgs> StatusChanged;
    }
}