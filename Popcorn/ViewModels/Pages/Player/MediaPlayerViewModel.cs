using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Extensions;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Services.Application;
using Popcorn.Utils;
using System.Collections.Generic;
using Popcorn.Models.Subtitles;
using System.Windows.Input;
using Popcorn.Helpers;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Popcorn.Chromecast.Models;
using Popcorn.Chromecast.Services;
using Popcorn.Services.Subtitles;
using Popcorn.Events;
using Popcorn.Services.Cache;
using Popcorn.Utils.Exceptions;

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
        /// Event fired on subtitle chosen
        /// </summary>
        public event EventHandler<SubtitleChangedEventArgs> SubtitleChosen;

        /// <summary>
        /// The media path
        /// </summary>
        public readonly string MediaPath;

        /// <summary>
        /// The media name
        /// </summary>
        public readonly string MediaName;

        private bool _canSeek;

        /// <summary>
        /// The media duration in seconds
        /// </summary>
        public double MediaDuration { get; set; }

        private Func<object, Task<object>> _castServer;

        private ChromecastReceiver _chromecastReceiver;

        private ICommand _playCastCommand;

        private ICommand _pauseCastCommand;

        private ICommand _seekCastCommand;

        private ICommand _stopCastCommand;

        private readonly IChromecastService _chromecastService;

        private readonly DispatcherTimer _castPlayerTimer;

        public event EventHandler<TimeChangedEventArgs> CastPlayerTimeChanged;

        private CancellationTokenSource _castPlayerCancellationTokenSource;

        private double _playerTime;

        private bool _isCastPlaying;

        private bool _isCastPaused;

        private bool _isSubtitleChosen;

        private OSDB.Subtitle _currentSubtitle;

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
        /// Initializes a new instance of the MediaPlayerViewModel class.
        /// </summary>
        /// <param name="subtitlesService"></param>
        /// <param name="cacheService">Caching service</param>
        /// <param name="mediaPath">Media path</param>
        /// <param name="mediaName">Media name</param>
        /// <param name="type">Media type</param>
        /// <param name="mediaStoppedAction">Media action to execute when media has been stopped</param>
        /// <param name="mediaEndedAction">Media action to execute when media has ended</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="bandwidthRate">THe bandwidth rate</param>
        /// <param name="subtitleFilePath">Subtitle file path</param>
        /// <param name="subtitles">Subtitles</param>
        public MediaPlayerViewModel(ISubtitlesService subtitlesService, ICacheService cacheService,
            string mediaPath,
            string mediaName, MediaType type, Action mediaStoppedAction,
            Action mediaEndedAction, Progress<double> bufferProgress = null,
            Progress<BandwidthRate> bandwidthRate = null, Subtitle currentSubtitle = null,
            IEnumerable<Subtitle> subtitles = null)
        {
            Logger.Info(
                $"Loading media : {mediaPath}.");
            RegisterCommands();
            _chromecastService = new ChromecastService();
            _subtitlesService = subtitlesService;
            _cacheService = cacheService;
            MediaPath = mediaPath;
            MediaName = mediaName;
            MediaType = type;
            CanSeek = true;
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            SubtitleFilePath = currentSubtitle?.FilePath;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;
            ShowSubtitleButton = MediaType != MediaType.Trailer;
            _subtitles = subtitles;
            _castPlayerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _castPlayerTimer.Tick += OnCastPlayerTimerChanged;

            if (currentSubtitle != null && currentSubtitle.Sub.SubtitleId != "none")
            {
                IsSubtitleChosen = true;
                CurrentSubtitle = currentSubtitle.Sub;
            }
        }

        /// <summary>
        /// Fire CastPlayerTimeChanged event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnCastPlayerTimeChanged(TimeChangedEventArgs e)
        {
            var handler = CastPlayerTimeChanged;
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

        private double _castTimeInSeconds;

        private void OnCastPlayerTimerChanged(object sender, EventArgs e)
        {
            _castTimeInSeconds += 1d;
            OnCastPlayerTimeChanged(new TimeChangedEventArgs(_castTimeInSeconds));
        }

        /// <summary>
        /// When a media has been ended, invoke the <see cref="_mediaEndedAction"/>
        /// </summary>
        public void MediaEnded()
        {
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
                    if (_castServer != null)
                    {
                        await _castServer.Invoke("play");
                    }
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
                    if (_castServer != null)
                    {
                        await _castServer.Invoke("pause");
                    }
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
                    if (_castServer != null)
                    {
                        await _castServer.Invoke($"seek:{seek}");
                    }
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
                        var path = Assembly.GetExecutingAssembly().Location;
                        var directory = Directory.GetParent(path);
                        OnSubtitleChosen(new SubtitleChangedEventArgs($@"{directory}\None.srt", message.SelectedSubtitle));
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
                            await StopCastPlayer();

                        _castPlayerTimer.Tick -= OnCastPlayerTimerChanged;
                        _mediaStoppedAction?.Invoke();
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
                    if (_castServer != null)
                    {
                        await StopCastPlayer();
                    }
                    else
                    {
                        IsCasting = false;
                        OnPausedMedia(new EventArgs());
                        var message =
                            new CastMediaMessage {CastCancellationTokenSource = new CancellationTokenSource()};
                        _castPlayerCancellationTokenSource = message.CastCancellationTokenSource;
                        message.StartCast = async chromecastReseiver =>
                        {
                            await LoadMedia(chromecastReseiver, message.CloseCastDialog);
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        private async Task StopCastPlayer()
        {
            try
            {
                IsCasting = false;
                OnCastStopped(new EventArgs());
                OnResumedMedia(new EventArgs());
                if (_castServer != null)
                {
                    await _castServer.Invoke("stop");
                    _castServer = null;
                }
            }
            catch (Exception ex)
            {
                _castServer = null;
                Logger.Error(ex);
            }
        }

        public async Task SetVolume(double volume)
        {
            try
            {
                if (_castServer != null)
                {
                    await _castServer.Invoke($"volume:{volume}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task LoadMedia(ChromecastReceiver chromecast, Action closeCastDialog)
        {
            Uri uriResult;
            var isRemote = Uri.TryCreate(MediaPath, UriKind.Absolute, out uriResult)
                           && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            CanSeek = isRemote;
            var session = new ChromecastSession
            {
                SourceType = isRemote ? SourceType.Youtube : SourceType.Torrent,
                Chromecast = chromecast,
                MediaPath = MediaPath,
                MediaTitle = MediaName,
                SubtitlePath = SubtitleFilePath,
                OnCastFailed = async message =>
                {
                    if (!_castPlayerCancellationTokenSource.IsCancellationRequested)
                    {
                        closeCastDialog.Invoke();
                        Messenger.Default.Send(
                            new UnhandledExceptionMessage(
                                new PopcornException(
                                    LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                    }

                    OnCastStopped(new EventArgs());
                    await StopCastPlayer();
                    return await Task.FromResult(message);
                },
                OnStatusChanged = async message =>
                {
                    var dict = message as IDictionary<string, object>;
                    object state;
                    if (dict.TryGetValue("playerState", out state) &&
                        string.Equals((string) state, "PLAYING"))
                    {
                        IsCastPaused = false;
                        IsCastPlaying = true;
                        try
                        {
                            if (dict["currentTime"] is double)
                            {
                                _castTimeInSeconds = (double)dict["currentTime"];
                                _castPlayerTimer.Start();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                    else if (dict.TryGetValue("playerState", out state) &&
                             string.Equals((string) state, "PAUSED"))
                    {
                        IsCastPaused = true;
                        IsCastPlaying = false;
                        try
                        {
                            if (dict["currentTime"] is double)
                            {
                                _castTimeInSeconds = (double) dict["currentTime"];
                                _castPlayerTimer.Stop();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }

                    return await Task.FromResult(message);
                },
                OnCastSarted = async message =>
                {
                    if (!_castPlayerCancellationTokenSource.IsCancellationRequested)
                    {
                        closeCastDialog.Invoke();
                        OnCastStarted(new EventArgs());
                        IsCasting = true;
                        await _castServer.Invoke($"seek:{PlayerTime}");
                    }
                    else
                    {
                        IsCasting = false;
                    }

                    OnResumedMedia(new EventArgs());
                    return await Task.FromResult(message);
                }
            };

            try
            {
                var castSession = await _chromecastService.StartCastAsync(session);
                _castServer = castSession.CastServer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                closeCastDialog.Invoke();
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(
                            LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                OnCastStopped(new EventArgs());
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

        public bool CanSeek
        {
            get { return _canSeek; }
            set { Set(ref _canSeek, value); }
        }

        public double PlayerTime
        {
            get { return _playerTime; }
            set { Set(ref _playerTime, value); }
        }

        public ChromecastReceiver ChromecastReceiver
        {
            get { return _chromecastReceiver; }
            set { Set(ref _chromecastReceiver, value); }
        }

        public bool IsCastPlaying
        {
            get { return _isCastPlaying; }
            set { Set(ref _isCastPlaying, value); }
        }

        public bool IsCastPaused
        {
            get { return _isCastPaused; }
            set { Set(ref _isCastPaused, value); }
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
            var handler = SubtitleChosen;
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