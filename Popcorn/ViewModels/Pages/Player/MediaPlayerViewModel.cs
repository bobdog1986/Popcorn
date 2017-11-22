using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Extensions;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Utils;
using System.Collections.Generic;
using Popcorn.Models.Subtitles;
using System.Windows.Input;
using Popcorn.Helpers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using GoogleCast;
using GoogleCast.Models.Media;
using Popcorn.Services.Subtitles;
using Popcorn.Events;
using Popcorn.Models.Chromecast;
using Popcorn.Models.Download;
using Popcorn.Services.Cache;
using Popcorn.Services.Chromecast;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Pages.Home.Movie.Details;
using SubtitlesParser.Classes;

namespace Popcorn.ViewModels.Pages.Player
{
    /// <summary>
    /// Manage media player
    /// </summary>
    public class MediaPlayerViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Show subtitle button
        /// </summary>
        private bool _showSubtitleButton;

        /// <summary>
        /// Command used to stop playing the media
        /// </summary>
        public ICommand StopPlayingMediaCommand { get; set; }

        /// <summary>
        /// Command used to cast media
        /// </summary>
        public ICommand CastCommand { get; set; }

        /// <summary>
        /// Command used to select subtitles
        /// </summary>
        public ICommand SelectSubtitlesCommand { get; set; }

        /// <summary>
        /// Event fired on stopped playing the media
        /// </summary>
        public event EventHandler<EventArgs> StoppedMedia;

        /// <summary>
        /// Event fired on stopped playing the media
        /// </summary>
        public event EventHandler<EventArgs> PausedMedia;

        /// <summary>
        /// Event fired on resume playing the media
        /// </summary>
        public event EventHandler<EventArgs> ResumedMedia;

        /// <summary>
        /// The media path
        /// </summary>
        public readonly string MediaPath;

        /// <summary>
        /// The media name
        /// </summary>
        public readonly string MediaName;

        /// <summary>
        /// The media duration in seconds
        /// </summary>
        public double MediaDuration { get; set; }

        private IReceiver _chromecastReceiver;

        private ICommand _playCastCommand;

        private ICommand _pauseCastCommand;

        private ICommand _seekCastCommand;

        private ICommand _stopCastCommand;

        private readonly IChromecastService _chromecastService;

        private double _playerTime;

        private bool _isSubtitleChosen;

        private OSDB.Subtitle _currentSubtitle;

        public IEnumerable<SubtitleItem> SubtitleItems = new List<SubtitleItem>();

        /// <summary>
        /// Media action to execute when media has ended
        /// </summary>
        private readonly Action _mediaEndedAction;

        /// <summary>
        /// Media action to execute when media has been stopped
        /// </summary>
        private readonly Action _mediaStoppedAction;

        /// <summary>
        /// Subtitle file path
        /// </summary>
        public readonly string SubtitleFilePath;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// The download rate
        /// </summary>
        public readonly Progress<BandwidthRate> BandwidthRate;

        /// <summary>
        /// Is casting
        /// </summary>
        private bool _isCasting;

        /// <summary>
        /// The media type
        /// </summary>
        public readonly MediaType MediaType;

        public event EventHandler<EventArgs> CastStarted;

        public event EventHandler<EventArgs> CastStopped;

        public event EventHandler<MediaStatusEventArgs> CastStatusChanged;

        /// <summary>
        /// The cache service
        /// </summary>
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Show subtitle button
        /// </summary>
        public bool ShowSubtitleButton
        {
            get { return _showSubtitleButton; }
            set { Set(ref _showSubtitleButton, value); }
        }

        /// <summary>
        /// Subtitle service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// Subtitles
        /// </summary>
        private readonly IEnumerable<Subtitle> _subtitles;

        /// <summary>
        /// The playing progress
        /// </summary>
        private readonly IProgress<double> _playingProgress;

        /// <summary>
        /// The piece availability progress
        /// </summary>
        public readonly Progress<PieceAvailability> PieceAvailability;

        /// <summary>
        /// Initializes a new instance of the MediaPlayerViewModel class.
        /// </summary>
        /// <param name="chromecastService">The Chromecast service</param>
        /// <param name="subtitlesService"></param>
        /// <param name="cacheService">Caching service</param>
        /// <param name="mediaPath">Media path</param>
        /// <param name="mediaName">Media name</param>
        /// <param name="type">Media type</param>
        /// <param name="mediaStoppedAction">Media action to execute when media has been stopped</param>
        /// <param name="mediaEndedAction">Media action to execute when media has ended</param>
        /// <param name="playingProgress">Media playing progress</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="pieceAvailability">The piece availability</param>
        /// <param name="bandwidthRate">THe bandwidth rate</param>
        /// <param name="currentSubtitle">Subtitle</param>
        /// <param name="subtitles">Subtitles</param>
        public MediaPlayerViewModel(IChromecastService chromecastService, ISubtitlesService subtitlesService,
            ICacheService cacheService,
            string mediaPath,
            string mediaName, MediaType type, Action mediaStoppedAction,
            Action mediaEndedAction, IProgress<double> playingProgress = null, Progress<double> bufferProgress = null,
            Progress<PieceAvailability> pieceAvailability = null,
            Progress<BandwidthRate> bandwidthRate = null, Subtitle currentSubtitle = null,
            IEnumerable<Subtitle> subtitles = null)
        {
            Logger.Info(
                $"Loading media : {mediaPath}.");
            RegisterCommands();
            _chromecastService = chromecastService;
            _chromecastService.StatusChanged += OnCastMediaStatusChanged;
            _subtitlesService = subtitlesService;
            _cacheService = cacheService;
            MediaPath = mediaPath;
            MediaName = mediaName;
            MediaType = type;
            PieceAvailability = pieceAvailability;
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            SubtitleFilePath = currentSubtitle?.FilePath;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;
            ShowSubtitleButton = MediaType != MediaType.Trailer;
            _subtitles = subtitles;
            _playingProgress = playingProgress;

            if (currentSubtitle != null && currentSubtitle.Sub.SubtitleId != "none" &&
                !string.IsNullOrEmpty(currentSubtitle.FilePath))
            {
                IsSubtitleChosen = true;
                CurrentSubtitle = currentSubtitle.Sub;
                SubtitleItems = _subtitlesService.LoadCaptions(currentSubtitle.FilePath);
            }
        }

        /// <summary>
        /// Occurs when cast media status has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCastMediaStatusChanged(object sender, MediaStatusEventArgs e)
        {
            CastStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Fire CastStopped event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnCastStopped(EventArgs e)
        {
            var handler = CastStopped;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Fire CastStarted event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnCastStarted(EventArgs e)
        {
            var handler = CastStarted;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// When a media has been ended, invoke the <see cref="_mediaEndedAction"/>
        /// </summary>
        public void MediaEnded()
        {
            if (MediaType == MediaType.Movie)
            {
                try
                {
                    var movieDetails = SimpleIoc.Default.GetInstance<MovieDetailsViewModel>();
                    movieDetails.SetWatchedMovieCommand.Execute(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            StopPlayingMediaCommand.Execute(null);
        }

        public bool IsCasting
        {
            get => _isCasting;
            set => Set(ref _isCasting, value);
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            PlayCastCommand = new RelayCommand(async () =>
            {
                try
                {
                    await _chromecastService.PlayAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });

            PauseCastCommand = new RelayCommand(async () =>
            {
                try
                {
                    await _chromecastService.PauseAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });

            SeekCastCommand = new RelayCommand<double>(async seek =>
            {
                try
                {
                    await _chromecastService.SeekAsync(seek);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });

            StopCastCommand = new RelayCommand(async () => { await StopCastPlayer(); });

            SelectSubtitlesCommand = new RelayCommand(async () =>
            {
                IsSubtitleChosen = !IsSubtitleChosen;
                var previousSubtitleChosen = IsSubtitleChosen;
                try
                {
                    IsSubtitleChosen = false;
                    OnPausedMedia(new EventArgs());
                    var message = new ShowSubtitleDialogMessage(_subtitles, CurrentSubtitle);
                    await Messenger.Default.SendAsync(message);
                    if (message.SelectedSubtitle != null &&
                        message.SelectedSubtitle.LanguageName !=
                        LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") &&
                        message.SelectedSubtitle.SubtitleId != "custom")
                    {
                        OnResumedMedia(new EventArgs());
                        if (CurrentSubtitle != null && CurrentSubtitle.ImdbId == message.SelectedSubtitle.ImdbId)
                        {
                            IsSubtitleChosen = true;
                        }
                        else
                        {
                            var path = Path.Combine(_cacheService.Subtitles + message.SelectedSubtitle.ImdbId);
                            Directory.CreateDirectory(path);
                            var subtitlePath = await
                                _subtitlesService.DownloadSubtitleToPath(path,
                                    message.SelectedSubtitle);
                            OnSubtitleChosen(new SubtitleChangedEventArgs(subtitlePath, message.SelectedSubtitle));
                            IsSubtitleChosen = true;
                        }
                    }
                    else if (message.SelectedSubtitle != null &&
                             message.SelectedSubtitle.LanguageName !=
                             LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") &&
                             message.SelectedSubtitle.SubtitleId == "custom")
                    {
                        var subMessage = new CustomSubtitleMessage();
                        await Messenger.Default.SendAsync(subMessage);
                        if (!subMessage.Error && !string.IsNullOrEmpty(subMessage.FileName))
                        {
                            OnSubtitleChosen(
                                new SubtitleChangedEventArgs(subMessage.FileName, message.SelectedSubtitle));
                            IsSubtitleChosen = true;
                        }
                        else
                        {
                            IsSubtitleChosen = false;
                        }

                        OnResumedMedia(new EventArgs());
                    }
                    else if (message.SelectedSubtitle != null && message.SelectedSubtitle.LanguageName ==
                             LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"))
                    {
                        OnSubtitleChosen(new SubtitleChangedEventArgs(string.Empty, message.SelectedSubtitle));
                        OnResumedMedia(new EventArgs());
                        IsSubtitleChosen = false;
                    }
                    else
                    {
                        OnResumedMedia(new EventArgs());
                        IsSubtitleChosen = previousSubtitleChosen;
                    }
                }
                catch (Exception ex)
                {
                    IsSubtitleChosen = previousSubtitleChosen;
                    Logger.Trace(ex);
                }
            });

            StopPlayingMediaCommand =
                new RelayCommand(async () =>
                {
                    try
                    {
                        if (IsCasting)
                            await StopCastPlayer(false);

                        _mediaStoppedAction?.Invoke();
                        _chromecastService.StatusChanged -= OnCastMediaStatusChanged;
                        OnStoppedMedia(new EventArgs());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                });

            CastCommand = new RelayCommand(async () =>
            {
                try
                {
                    if (!_chromecastService.IsStopped)
                    {
                        await StopCastPlayer();
                    }
                    else
                    {
                        IsCasting = false;
                        OnPausedMedia(new EventArgs());
                        var message =
                            new CastMediaMessage {CastCancellationTokenSource = new CancellationTokenSource()};
                        message.StartCast = async chromecastReseiver =>
                        {
                            await LoadCastAsync(message.CloseCastDialog);
                        };
                        await Messenger.Default.SendAsync(message);
                        if (message.CastCancellationTokenSource.IsCancellationRequested)
                        {
                            await StopCastPlayer();
                        }
                        else if (message.ChromecastReceiver == null)
                        {
                            OnResumedMedia(new EventArgs());
                        }
                        else
                        {
                            ChromecastReceiver = message.ChromecastReceiver;
                            IsCasting = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        private async Task StopCastPlayer(bool resume = true)
        {
            try
            {
                IsCasting = false;
                OnCastStopped(new EventArgs());
                if (resume)
                    OnResumedMedia(new EventArgs());
                await _chromecastService.StopAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task SetVolume(float volume)
        {
            try
            {
                await _chromecastService.SetVolumeAsync(volume);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private string GetLocalIpAddress()
        {
            string localIp;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIp = endPoint.Address.ToString();
            }
            return localIp;
        }

        private async Task LoadCastAsync(Action closeCastDialog)
        {
            var isRemote = Uri.TryCreate(MediaPath, UriKind.Absolute, out var uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            var videoPath = MediaPath.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries).Last()?
                .Replace("\\", "/");
            var mediaPath = $"http://{GetLocalIpAddress()}:9900/{videoPath}";
            var subtitle = SubtitleFilePath;
            if (!string.IsNullOrEmpty(subtitle))
            {
                subtitle = _subtitlesService.ConvertSrtToVtt(subtitle);
                if (subtitle != null)
                {
                    subtitle = subtitle.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries).Last()?
                        .Replace("\\", "/");
                    subtitle = $"http://{GetLocalIpAddress()}:9900/{subtitle}";
                }
            }

            var media = new Media
            {
                ContentId = isRemote ? MediaPath : mediaPath,
                ContentType = "video/mp4",
                Metadata = new MovieMetadata
                {
                    Title = MediaName,
                    SubTitle = !string.IsNullOrEmpty(subtitle) ? subtitle : null
                }
            };

            try
            {
                await _chromecastService.LoadAsync(media);
                closeCastDialog.Invoke();
                OnCastStarted(new EventArgs());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                closeCastDialog.Invoke();
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(
                            LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                OnResumedMedia(new EventArgs());
                IsCasting = false;
                await StopCastPlayer();
            }
        }

        public ICommand PlayCastCommand
        {
            get { return _playCastCommand; }
            set { Set(ref _playCastCommand, value); }
        }

        public ICommand PauseCastCommand
        {
            get { return _pauseCastCommand; }
            set { Set(ref _pauseCastCommand, value); }
        }

        public ICommand SeekCastCommand
        {
            get { return _seekCastCommand; }
            set { Set(ref _seekCastCommand, value); }
        }

        public ICommand StopCastCommand
        {
            get { return _stopCastCommand; }
            set { Set(ref _stopCastCommand, value); }
        }

        public OSDB.Subtitle CurrentSubtitle
        {
            get { return _currentSubtitle; }
            set { Set(ref _currentSubtitle, value); }
        }

        public bool IsSubtitleChosen
        {
            get { return _isSubtitleChosen; }
            set { Set(ref _isSubtitleChosen, value); }
        }

        public double MediaLength { get; set; }

        public double PlayerTime
        {
            get { return _playerTime; }
            set
            {
                Set(ref _playerTime, value);
                _playingProgress?.Report(value / MediaLength);
            }
        }

        public IReceiver ChromecastReceiver
        {
            get { return _chromecastReceiver; }
            set { Set(ref _chromecastReceiver, value); }
        }

        /// <summary>
        /// Fire ResumedMedia event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnResumedMedia(EventArgs e)
        {
            Logger.Debug(
                "Resumed playing a media");

            var handler = ResumedMedia;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Fire OnSubtitleChosen event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnSubtitleChosen(SubtitleChangedEventArgs e)
        {
            Logger.Debug(
                "Subtitle chosen");

            CurrentSubtitle = e.Subtitle;
            SubtitleItems = _subtitlesService.LoadCaptions(e.SubtitlePath);
        }

        /// <summary>
        /// Fire PausedMedia event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnPausedMedia(EventArgs e)
        {
            Logger.Debug(
                "Pause playing a media");

            var handler = PausedMedia;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Fire StoppedMedia event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnStoppedMedia(EventArgs e)
        {
            Logger.Debug(
                "Stop playing a media");

            var handler = StoppedMedia;
            handler?.Invoke(this, e);
        }
    }
}