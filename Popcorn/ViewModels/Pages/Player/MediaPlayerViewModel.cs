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
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Input;
using Popcorn.Helpers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Threading;
using GoogleCast;
using GoogleCast.Models.Media;
using OSDB.Models;
using Popcorn.Services.Subtitles;
using Popcorn.Events;
using Popcorn.Models.Chromecast;
using Popcorn.Services.Cache;
using Popcorn.Services.Chromecast;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Pages.Home.Movie.Details;

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
        /// The Chromecast service
        /// </summary>
        private readonly IChromecastService _chromecastService;

        /// <summary>
        /// <see cref="ShowSubtitleButton"/>
        /// </summary>
        private bool _showSubtitleButton;

        /// <summary>
        /// <see cref="Volume"/>
        /// </summary>
        private double _volume;

        /// <summary>
        /// <see cref="ChromecastReceiver"/>
        /// </summary>
        private IReceiver _chromecastReceiver;

        /// <summary>
        /// <see cref="PlayCastCommand"/>
        /// </summary>
        private ICommand _playCastCommand;

        /// <summary>
        /// <see cref="PauseCastCommand"/>
        /// </summary>
        private ICommand _pauseCastCommand;

        /// <summary>
        /// <see cref="SeekCastCommand"/>
        /// </summary>
        private ICommand _seekCastCommand;

        /// <summary>
        /// <see cref="StopCastCommand"/>
        /// </summary>
        private ICommand _stopCastCommand;

        /// <summary>
        /// <see cref="PlayerTime"/>
        /// </summary>
        private double _playerTime;

        /// <summary>
        /// <see cref="CurrentSubtitle"/>
        /// </summary>
        private Subtitle _currentSubtitle;

        /// <summary>
        /// <see cref="IsSeeking"/>
        /// </summary>
        private bool _isSeeking;

        /// <summary>
        /// <see cref="MediaLength"/>
        /// </summary>
        private double _mediaLength;

        /// <summary>
        /// <see cref="IsDragging"/>
        /// </summary>
        private bool _isDragging;

        /// <summary>
        /// Media action to execute when media has ended
        /// </summary>
        private readonly Action _mediaEndedAction;

        /// <summary>
        /// Media action to execute when media has been stopped
        /// </summary>
        private readonly Action _mediaStoppedAction;

        /// <summary>
        /// <see cref="IsCasting"/>
        /// </summary>
        private bool _isCasting;

        /// <summary>
        /// <see cref="MediaType"/>
        /// </summary>
        private MediaType _mediaType;

        /// <summary>
        /// <see cref="IsSubtitleChosen"/>
        /// </summary>
        private bool _isSubtitleChosen;

        /// <summary>
        /// Subtitles
        /// </summary>
        private ObservableCollection<Subtitle> _subtitles;

        /// <summary>
        /// Event raised when Chromecast broadcast has started
        /// </summary>
        public event EventHandler<EventArgs> CastStarted;

        /// <summary>
        /// Event raised when Chromecast broadcast has stopped
        /// </summary>
        public event EventHandler<EventArgs> CastStopped;

        /// <summary>
        /// Event raised when <see cref="MediaStatus"/> has changed
        /// </summary>
        public event EventHandler<MediaStatusEventArgs> CastStatusChanged;

        /// <summary>
        /// The cache service
        /// </summary>
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Subtitle service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// The playing progress
        /// </summary>
        private readonly IProgress<double> _playingProgress;

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
        /// <param name="bandwidthRate">THe bandwidth rate</param>
        /// <param name="currentSubtitle">Subtitle</param>
        /// <param name="subtitles">Subtitles</param>
        public MediaPlayerViewModel(IChromecastService chromecastService, ISubtitlesService subtitlesService,
            ICacheService cacheService,
            string mediaPath,
            string mediaName, MediaType type, Action mediaStoppedAction,
            Action mediaEndedAction, IProgress<double> playingProgress = null, Progress<double> bufferProgress = null,
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
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;
            ShowSubtitleButton = MediaType != MediaType.Trailer;
            _playingProgress = playingProgress;
            _subtitles = new ObservableCollection<Subtitle>();
            if (subtitles != null)
            {
                _subtitles = new ObservableCollection<Subtitle>(subtitles);
            }

            if (currentSubtitle != null && currentSubtitle.LanguageName != LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") &&
                !string.IsNullOrEmpty(currentSubtitle.FilePath))
            {
                CurrentSubtitle = currentSubtitle;
            }
        }

        /// <summary>
        /// The media path
        /// </summary>
        public readonly string MediaPath;

        /// <summary>
        /// The media name
        /// </summary>
        public readonly string MediaName;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// The download rate
        /// </summary>
        public readonly Progress<BandwidthRate> BandwidthRate;

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
        /// Event fired when subtitle changed
        /// </summary>
        public event EventHandler<SubtitleChangedEventArgs> SubtitleChanged;

        /// <summary>
        /// Command used to stop playing the media
        /// </summary>
        public ICommand StopPlayingMediaCommand { get; set; }

        /// <summary>
        /// Command used to cast media
        /// </summary>
        public ICommand CastCommand { get; set; }

        /// <summary>
        /// The media duration in seconds
        /// </summary>
        public double MediaDuration { get; set; }

        /// <summary>
        /// True if currently broadcasting the media through Chromecast
        /// </summary>
        public bool IsCasting
        {
            get => _isCasting;
            set => Set(ref _isCasting, value);
        }

        /// <summary>
        /// True if user is dragging the media player slider
        /// </summary>
        public bool IsDragging
        {
            get => _isDragging;
            set => Set(ref _isDragging, value);
        }

        /// <summary>
        /// True if media is seeking
        /// </summary>
        public bool IsSeeking
        {
            get => _isSeeking;
            set => Set(ref _isSeeking, value);
        }

        /// <summary>
        /// The media type
        /// </summary>
        public MediaType MediaType
        {
            get => _mediaType;
            set => Set(ref _mediaType, value);
        }

        /// <summary>
        /// Show subtitle button
        /// </summary>
        public bool ShowSubtitleButton
        {
            get { return _showSubtitleButton; }
            set { Set(ref _showSubtitleButton, value); }
        }

        /// <summary>
        /// Command used to play a media in Chromecast
        /// </summary>
        public ICommand PlayCastCommand
        {
            get => _playCastCommand;
            private set => Set(ref _playCastCommand, value);
        }

        /// <summary>
        /// Command used to pause the Chromecast broadcasting
        /// </summary>
        public ICommand PauseCastCommand
        {
            get => _pauseCastCommand;
            private set => Set(ref _pauseCastCommand, value);
        }

        /// <summary>
        /// Command which seeks the media in Chromecast
        /// </summary>
        public ICommand SeekCastCommand
        {
            get => _seekCastCommand;
            private set => Set(ref _seekCastCommand, value);
        }

        /// <summary>
        /// Command which stops the media
        /// </summary>
        public ICommand StopCastCommand
        {
            get => _stopCastCommand;
            private set => Set(ref _stopCastCommand, value);
        }

        
        /// <summary>
        /// True if subtitle has been chosen
        /// </summary>
        public bool IsSubtitleChosen
        {
            get => _isSubtitleChosen;
            set => Set(ref _isSubtitleChosen, value);
        }

        /// <summary>
        /// Current subtitle for the media
        /// </summary>
        public ObservableCollection<Subtitle> Subtitles
        {
            get => _subtitles;
            set => Set(ref _subtitles, value);
        }

        /// <summary>
        /// Current subtitle for the media
        /// </summary>
        public Subtitle CurrentSubtitle
        {
            get => _currentSubtitle;
            set
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    await ChangeSubtitle(value);
                    Set(ref _currentSubtitle, value);
                });
            }
        }

        /// <summary>
        /// The media length in seconds
        /// </summary>
        public double MediaLength
        {
            get => _mediaLength;
            set => Set(ref _mediaLength, value);
        }

        /// <summary>
        /// The media player progress in seconds
        /// </summary>
        public double PlayerTime
        {
            get => _playerTime;
            set
            {
                Set(ref _playerTime, value);
                _playingProgress?.Report(value / MediaLength);
            }
        }

        /// <summary>
        /// Get or set the Chromecast receiver
        /// </summary>
        public IReceiver ChromecastReceiver
        {
            get => _chromecastReceiver;
            set => Set(ref _chromecastReceiver, value);
        }

        /// <summary>
        /// Get or set the media volume between 0 and 1
        /// </summary>
        public double Volume
        {
            get => _volume;
            set
            {
                Set(ref _volume, value);
                if (IsCasting)
                {
                    Task.Run(async () => { await SetVolume(Convert.ToSingle(value)); });
                }
            }
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
                            new ShowCastMediaMessage {CastCancellationTokenSource = new CancellationTokenSource()};
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

        public async Task ChangeSubtitle(Subtitle subtitle)
        {
            try
            {
                if (subtitle.LanguageName !=
                    LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") &&
                    subtitle.LanguageName != LocalizationProviderHelper.GetLocalizedValue<string>("CustomLabel"))
                {
                    if (CurrentSubtitle != null &&
                        CurrentSubtitle.ISO639 == subtitle.ISO639)
                    {
                        return;
                    }

                    var path = Path.Combine(_cacheService.Subtitles + subtitle.IDMovieImdb);
                    Directory.CreateDirectory(path);
                    var subtitlePath = await
                        _subtitlesService.DownloadSubtitleToPath(path, subtitle);
                    subtitle.FilePath = _subtitlesService.LoadCaptions(subtitlePath);
                    OnSubtitleChosen(new SubtitleChangedEventArgs(subtitle));
                }
                else if (subtitle.LanguageName == LocalizationProviderHelper.GetLocalizedValue<string>("CustomLabel"))
                {
                    var subMessage = new ShowCustomSubtitleMessage();
                    await Messenger.Default.SendAsync(subMessage);
                    if (!subMessage.Error && !string.IsNullOrEmpty(subMessage.FileName))
                    {
                        subtitle.FilePath = subMessage.FileName;
                        OnSubtitleChosen(
                            new SubtitleChangedEventArgs(subtitle));
                    }
                }
                else if (subtitle.LanguageName ==
                         LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"))
                {
                    OnSubtitleChosen(new SubtitleChangedEventArgs(subtitle));
                }
            }
            catch (Exception ex)
            {
                Logger.Trace(ex);
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
        /// When a media has been ended, invoke the <see cref="StopPlayingMediaCommand"/>
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

        /// <summary>
        /// Stop Chromecast broadcasting
        /// </summary>
        /// <param name="resume">If should resume media player in PlayerUserControl</param>
        /// <returns><see cref="Task"/></returns>
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

        /// <summary>
        /// Set Chromecast volume
        /// </summary>
        /// <param name="volume">Volume to set</param>
        /// <returns><see cref="Task"/></returns>
        private async Task SetVolume(float volume)
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

        /// <summary>
        /// Load cast dialog
        /// </summary>
        /// <param name="closeCastDialog">Close the dialog when invoked</param>
        /// <returns><see cref="Task"/></returns>
        private async Task LoadCastAsync(Action closeCastDialog)
        {
            var isRemote = Uri.TryCreate(MediaPath, UriKind.Absolute, out var uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            var videoPath = MediaPath.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries).Last()?
                .Replace("\\", "/");
            var mediaPath = $"http://{Helper.GetLocalIpAddress()}:{Constants.ServerPort}/{videoPath}";
            var subtitle = CurrentSubtitle.FilePath;
            if (!string.IsNullOrEmpty(subtitle))
            {
                subtitle = _subtitlesService.ConvertSrtToVtt(subtitle);
                if (subtitle != null)
                {
                    subtitle = subtitle.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries).Last()?
                        .Replace("\\", "/");
                    subtitle = $"http://{Helper.GetLocalIpAddress()}:{Constants.ServerPort}/{subtitle}";
                }
            }

            var media = new MediaInformation
            {
                ContentId = isRemote ? MediaPath : mediaPath,
                ContentType = "video/mp4",

                Metadata = new MovieMetadata
                {
                    Title = MediaName
                }
            };

            if (!string.IsNullOrEmpty(subtitle))
            {
                media.Tracks = new[]
                {
                    new Track {TrackId = 1, Language = "en-US", Name = "English", TrackContentId = subtitle}
                };
                media.TextTrackStyle = new TextTrackStyle
                {
                    BackgroundColor = Color.Transparent,
                    EdgeColor = Color.Black,
                    EdgeType = TextTrackEdgeType.DropShadow
                };
            }

            try
            {
                await _chromecastService.LoadAsync(media, (!string.IsNullOrEmpty(subtitle), 1));
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
        /// Fire OnSubtitleChosen event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnSubtitleChosen(SubtitleChangedEventArgs e)
        {
            Logger.Debug(
                "Subtitle chosen");

            IsSubtitleChosen = !string.IsNullOrEmpty(e.Subtitle.FilePath);
            var handler = SubtitleChanged;
            handler?.Invoke(this, e);
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