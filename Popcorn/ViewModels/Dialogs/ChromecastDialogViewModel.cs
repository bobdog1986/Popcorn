using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MimeKit;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Services.FileServer;
using Popcorn.Utils.Exceptions;
using SharpCaster.Controllers;
using SharpCaster.Models;
using SharpCaster.Models.ChromecastStatus;
using SharpCaster.Models.MediaStatus;
using SharpCaster.Models.Metadata;
using SharpCaster.Services;

namespace Popcorn.ViewModels.Dialogs
{
    public class ChromecastDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private readonly IFileServerService _fileServerService;

        private readonly string _mediaPath;

        private readonly string _subtitleFilePath;

        private bool _loadingChromecasts;

        private ObservableCollection<Chromecast> _chromecasts;

        private ICommand _cancelCommand;

        private ICommand _closeCommand;

        private Chromecast _selectedDevice;

        private bool _anyChromecast;

        public Action OnCloseAction { get; set; }

        private readonly ChromecastService _chromecastService = ChromecastService.Current;

        private readonly DispatcherTimer _secondsTimer;

        private SharpCasterDemoController _controller;

        private Func<object, Task<object>> _mediaServer;

        private Func<object, Task<object>> _subtitleServer;

        private bool _connectingToChromecast;

        public ChromecastDialogViewModel(string title, string mediaPath, string subtitleFilePath,
            IFileServerService fileServerService)
        {
            _subtitleFilePath = subtitleFilePath;
            _fileServerService = fileServerService;
            _mediaPath = mediaPath;
            Title = title;
            Chromecasts = new ObservableCollection<Chromecast>();
            _chromecastService.ChromeCastClient.ApplicationStarted += ApplicationStarted;
            _chromecastService.ChromeCastClient.VolumeChanged += VolumeChanged;
            _chromecastService.ChromeCastClient.MediaStatusChanged += MediaStatusChanged;
            _chromecastService.ChromeCastClient.ConnectedChanged += ConnectedChanged;
            _secondsTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _secondsTimer.Tick += SecondsTimer_Tick;
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
               await LoadChromecasts();
            });

            CloseCommand = new RelayCommand(() =>
            {
                OnCloseAction.Invoke();
            });

            CancelCommand = new RelayCommand(async () =>
            {
                await StopApplication();
                if (_mediaServer != null)
                    await _mediaServer(null);

                if (_subtitleServer != null)
                    await _subtitleServer(null);
            });
        }

        private IPAddress LocalIPAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private async Task LoadChromecasts()
        {
            try
            {
                LoadingChromecasts = true;
                var ip = LocalIPAddress().ToString();
                var foundChromecasts = await _chromecastService.StartLocatingDevices(ip);
                foreach (var foundChromecast in foundChromecasts)
                {
                    Chromecasts.Add(foundChromecast);
                }

                LoadingChromecasts = false;
                AnyChromecast = Chromecasts.Any();
            }
            catch (Exception ex)
            {
                LoadingChromecasts = false;
                AnyChromecast = false;
                Logger.Error(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                CancelCommand.Execute(null);
                CloseCommand.Execute(null);
            }
        }

        private void ConnectedChanged(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                _controller = await _chromecastService.ChromeCastClient.LaunchSharpCaster();
                ConnectingToChromecast = false;
                ConnectedToChromecast = _chromecastService.ChromeCastClient.Connected;
            });
        }

        private void SecondsTimer_Tick(object sender, object e)
        {
            Position += 1;
        }

        private void MediaStatusChanged(object sender, MediaStatus e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                switch (e.PlayerState)
                {
                    case PlayerState.Playing:
                        _secondsTimer.Start();
                        break;
                    default:
                        _secondsTimer.Stop();
                        break;
                }
                Position = e.CurrentTime;
                if (e.Media != null)
                    Length = e.Media.duration;
            });
        }

        private void VolumeChanged(object sender, Volume e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => { Volume = e.level * 100; });
        }

        private async void ApplicationStarted(object sender, ChromecastApplication e)
        {
            await LoadMedia(Title);
        }

        public async Task PlayPause()
        {
            try
            {
                if (_chromecastService.ChromeCastClient.MediaStatus != null &&
                    _chromecastService.ChromeCastClient.MediaStatus.PlayerState == PlayerState.Paused)
                {
                    await _controller.Play();
                }
                else
                {
                    await _controller.Pause();
                }
            }
            catch (Exception)
            {
            }
        }

        public bool CanPause => _chromecastService.ChromeCastClient.MediaStatus != null &&
                                _chromecastService.ChromeCastClient.MediaStatus.PlayerState == PlayerState.Playing;

        public bool CanPlay => _chromecastService.ChromeCastClient.MediaStatus != null &&
                               _chromecastService.ChromeCastClient.MediaStatus.PlayerState == PlayerState.Paused;

        public async Task Pause()
        {
            try
            {
                await _controller.Pause();
            }
            catch (Exception)
            {
            }
        }

        private async Task LoadMedia(string title)
        {
            while (_controller == null)
            {
                await Task.Delay(500);
            }

            Uri uriResult;
            var isRemote = Uri.TryCreate(_mediaPath, UriKind.Absolute, out uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            Track track = null;
            if (!string.IsNullOrEmpty(_subtitleFilePath))
            {
                var message = MimeMessage.Load(_subtitleFilePath);
                _subtitleServer = await _fileServerService.StartStaticFileServer(_subtitleFilePath,
                    message.BodyParts.FirstOrDefault().ContentType.Name, 9001);
                track = new Track
                {
                    Name = "Subtitle",
                    TrackId = 100,
                    Type = "TEXT",
                    SubType = "captions",
                    TrackContentId =
                        $"http://{LocalIPAddress()}:9001"
                };
            }

            var metadata = new GenericMediaMetadata {title = title};
            if (!isRemote)
            {
                try
                {
                    var message = MimeMessage.Load(_mediaPath);
                    _mediaServer =
                        await _fileServerService.StartStreamFileServer(_mediaPath,
                            message.BodyParts.FirstOrDefault().ContentType.Name, 9000);
                    await _controller.LoadMedia($"http://{LocalIPAddress()}:9000",
                        message.BodyParts.FirstOrDefault().ContentType.Name, metadata, "LIVE",
                        0D, null, track == null ? null : new[] {track}, new[] {100});
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    Messenger.Default.Send(
                        new UnhandledExceptionMessage(
                            new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                }
            }
            else
            {
                if (track != null)
                {
                    await _controller.LoadMedia(_mediaPath, "video/mp4", metadata, "BUFFERED", 0D,
                        null, new[] {track},
                        new[] {100});
                }
                else
                {
                    await _controller.LoadMedia(_mediaPath, "video/mp4", metadata, "BUFFERED");
                }
            }
        }

        public async Task Seek(double seconds)
        {
            try
            {
                if (Math.Abs(Position - seconds) > 0.1)
                    await _controller.Seek(seconds);
            }
            catch (Exception)
            {
            }
        }

        public async Task MuteUnmute()
        {
            try
            {
                await _controller.SetMute(!_chromecastService.ChromeCastClient.Volume.muted);
            }
            catch (Exception)
            {
            }
        }

        public async Task SetVolume(double newValue)
        {
            try
            {
                if (Math.Abs(_chromecastService.ChromeCastClient.Volume.level - (newValue / 100)) < 0.01) return;
                await _controller.SetVolume((float) (newValue / 100));
            }
            catch (Exception)
            {
            }
        }

        public async Task StopApplication()
        {
            try
            {
                if (_controller != null)
                    await _controller.StopApplication();
            }
            catch (Exception)
            {
            }
        }

        public bool ConnectedToChromecast
        {
            get => _connectedToChromecast;
            set => Set(ref _connectedToChromecast, value);
        }

        private bool _connectedToChromecast;

        public Chromecast SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                Set(ref _selectedDevice, value);

                ConnectingToChromecast = true;
                Task.Run(async () =>
                {
                    try
                    {
                        await ChromecastService.Current.ConnectToChromecast(value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        Messenger.Default.Send(
                            new UnhandledExceptionMessage(
                                new PopcornException(LocalizationProviderHelper
                                    .GetLocalizedValue<string>("CastFailed"))));
                        CancelCommand.Execute(null);
                        CloseCommand.Execute(null);
                    }
                });
            }
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => Set(ref _closeCommand, value);
        }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => Set(ref _cancelCommand, value);
        }

        public double Volume
        {
            get => _volume;
            set
            {
                Set(ref _volume, value);
                Task.Run(async () =>
                {
                    await SetVolume(value);
                });
            }
        }

        private double _volume;

        public double Length
        {
            get => _length;
            set => Set(ref _length, value);
        }

        private double _length;

        public double Position
        {
            get => _position;
            set => Set(ref _position, value);
        }

        private double _position;

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _title;

        public ObservableCollection<Chromecast> Chromecasts
        {
            get => _chromecasts;
            set => Set(ref _chromecasts, value);
        }

        public bool AnyChromecast
        {
            get => _anyChromecast;
            set => Set(ref _anyChromecast, value);
        }

        public bool ConnectingToChromecast
        {
            get => _connectingToChromecast;
            set => Set(ref _connectingToChromecast, value);
        }

        public bool LoadingChromecasts
        {
            get => _loadingChromecasts;
            set => Set(ref _loadingChromecasts, value);
        }
    }
}