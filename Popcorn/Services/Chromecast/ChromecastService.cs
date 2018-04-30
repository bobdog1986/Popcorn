using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using Popcorn.Models.Chromecast;

namespace Popcorn.Services.Chromecast
{
    public class ChromecastService : IChromecastService
    {
        private bool IsInitialized { get; set; }
        private IDeviceLocator DeviceLocator { get; }
        private ISender Sender { get; }
        private IReceiver Receiver { get; set; }

        public ChromecastService(IDeviceLocator deviceLocator, ISender sender)
        {
            DeviceLocator = deviceLocator;
            Sender = sender;
            sender.GetChannel<IMediaChannel>().StatusChanged += MediaChannelStatusChanged;
        }

        private async Task InvokeAsync<TChannel>(Func<TChannel, Task> action) where TChannel : IChannel
        {
            try
            {
                if (action != null)
                {
                    await action.Invoke(Sender.GetChannel<TChannel>());
                }
            }
            catch (Exception) { }
        }

        private async Task SendChannelCommandAsync<TChannel>(bool condition, Func<TChannel, Task> action,
            Func<TChannel, Task> otherwise) where TChannel : IChannel
        {
            await InvokeAsync<TChannel>(condition ? action : otherwise);
        }

        public async Task<IEnumerable<MediaStatus>> GetStatus()
        {
            try
            {
                return await Sender.GetChannel<IMediaChannel>().GetStatusAsync();
            }
            catch (Exception)
            {
                return new List<MediaStatus>();
            }
        }

        public bool IsStopped
        {
            get
            {
                var mediaChannel = Sender.GetChannel<IMediaChannel>();
                return (mediaChannel.Status == null ||
                        !String.IsNullOrEmpty(mediaChannel.Status.FirstOrDefault()?.IdleReason));
            }
        }

        public async Task<bool> ConnectAsync(IReceiver receiver)
        {
            try
            {
                Receiver = receiver;
                if (Receiver != null)
                {
                    await Sender.ConnectAsync(Receiver);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task LoadAsync(MediaInformation media, (bool hasSubtitle, int trackId) subtitle)
        {
            await SendChannelCommandAsync<IMediaChannel>(!IsInitialized || IsStopped,
                async c =>
                {
                    if (!string.IsNullOrWhiteSpace(media.ContentId))
                    {
                        if (await ConnectAsync(Receiver))
                        {
                            var sender = Sender;
                            var mediaChannel = sender.GetChannel<IMediaChannel>();
                            await sender.LaunchAsync(mediaChannel);
                            if (subtitle.hasSubtitle)
                            {
                                await mediaChannel.LoadAsync(media, true, subtitle.trackId);
                            }
                            else
                            {
                                await mediaChannel.LoadAsync(media);
                            }
                            IsInitialized = true;
                        }
                    }
                }, c => c.PlayAsync());
        }

        public async Task PauseAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.PauseAsync());
        }

        public async Task PlayAsync()
        {
            await InvokeAsync<IMediaChannel>(c => c.PlayAsync());
        }

        public async Task SeekAsync(double seconds)
        {
            await InvokeAsync<IMediaChannel>(c => c.SeekAsync(seconds));
        }

        public async Task StopAsync()
        {
            if (IsStopped)
            {
                if (IsInitialized || await ConnectAsync(Receiver))
                {
                    await InvokeAsync<IReceiverChannel>(c => c.StopAsync());
                }
            }
            else
            {
                await InvokeAsync<IMediaChannel>(c => c.StopAsync());
            }
        }

        public async Task<IEnumerable<IReceiver>> FindReceiversAsync()
        {
            try
            {
                return await DeviceLocator.FindReceiversAsync();
            }
            catch (Exception)
            {
                return new List<IReceiver>();
            }
        }

        private bool _isMuted;
        /// <summary>
        /// Gets or sets a value indicating whether the audio is muted
        /// </summary>
        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    Task.Run(async () =>
                    {
                        await SetIsMutedAsync();
                    });
                }
            }
        }

        public async Task SetVolumeAsync(float volume)
        {
            await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null, async c => await c.SetVolumeAsync(volume));
        }

        public async Task SetIsMutedAsync()
        {
            await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null,
                async c => await c.SetIsMutedAsync(IsMuted));
        }

        private void MediaChannelStatusChanged(object sender, EventArgs e)
        {
            var status = ((IMediaChannel) sender).Status?.FirstOrDefault();
            StatusChanged?.Invoke(sender, new MediaStatusEventArgs(status));
        }

        public event EventHandler<MediaStatusEventArgs> StatusChanged;
    }
}