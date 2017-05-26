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
using Popcorn.Services.Subtitles;
using Popcorn.Events;

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

        /// <summary>
        /// Show subtitle button
        /// </summary>
        public bool ShowSubtitleButton
        {
            get { return _showSubtitleButton; }
            set { Set(ref _showSubtitleButton, value);
            }
        }

        /// <summary>
        /// Application service
        /// </summary>
        private readonly IApplicationService _applicationService;

        /// <summary>
        /// Subtitle service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// Subtitles
        /// </summary>
        private IEnumerable<Subtitle> _subtitles;

        /// <summary>
        /// Custom subtitle
        /// </summary>
        public string CustomSubtitlePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the MediaPlayerViewModel class.
        /// </summary>
        /// <param name="applicationService">Application service</param>
        /// <param name="mediaPath">Media path</param>
        /// <param name="mediaName">Media name</param>
        /// <param name="type">Media type</param>
        /// <param name="mediaStoppedAction">Media action to execute when media has been stopped</param>
        /// <param name="mediaEndedAction">Media action to execute when media has ended</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="bandwidthRate">THe bandwidth rate</param>
        /// <param name="subtitleFilePath">Subtitle file path</param>
        /// <param name="subtitles">Subtitles</param>
        public MediaPlayerViewModel(ISubtitlesService subtitlesService,IApplicationService applicationService, string mediaPath,
            string mediaName, MediaType type, Action mediaStoppedAction,
            Action mediaEndedAction, Progress<double> bufferProgress = null,
            Progress<BandwidthRate> bandwidthRate = null, string subtitleFilePath = null, IEnumerable<Subtitle> subtitles = null)
        {
            Logger.Info(
                $"Loading media : {mediaPath}.");
            RegisterCommands();
            _subtitlesService = subtitlesService;
            _applicationService = applicationService;
            MediaPath = mediaPath;
            MediaName = mediaName;
            MediaType = type;
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            SubtitleFilePath = subtitleFilePath;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;
            ShowSubtitleButton = MediaType == MediaType.Trailer ? false : true;
            _subtitles = subtitles;
            // Prevent windows from sleeping
            _applicationService.EnableConstantDisplayAndPower(true);
        }

        /// <summary>
        /// When a media has been ended, invoke the <see cref="_mediaEndedAction"/>
        /// </summary>
        public void MediaEnded()
        {
            _mediaEndedAction?.Invoke();
            _applicationService.EnableConstantDisplayAndPower(false);
            OnStoppedMedia(new EventArgs());
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
            SelectSubtitlesCommand = new RelayCommand(async () =>
            {
                OnPausedMedia(new EventArgs());
                var message = new ShowSubtitleDialogMessage(_subtitles);
                await Messenger.Default.SendAsync(message);

                OnResumedMedia(new EventArgs());
                if (message.SelectedSubtitle != null &&
                    message.SelectedSubtitle.LanguageName !=
                    LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") && message.SelectedSubtitle.SubtitleId != "custom")
                {
                    var path = Path.Combine(Constants.Subtitles + message.SelectedSubtitle.ImdbId);
                    Directory.CreateDirectory(path);
                    var subtitlePath = await
                        _subtitlesService.DownloadSubtitleToPath(path,
                            message.SelectedSubtitle);
                    OnSubtitleChosen(new SubtitleChangedEventArgs(subtitlePath));
                }
                else if (message.SelectedSubtitle != null &&
                    message.SelectedSubtitle.LanguageName !=
                    LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel") && message.SelectedSubtitle.SubtitleId == "custom")
                {
                    var subMessage = new CustomSubtitleMessage();
                    await Messenger.Default.SendAsync(subMessage);
                    if (!subMessage.Error && !string.IsNullOrEmpty(subMessage.FileName))
                    {
                        OnSubtitleChosen(new SubtitleChangedEventArgs(subMessage.FileName));
                    }
                }
            });

            StopPlayingMediaCommand =
                new RelayCommand(() =>
                {
                    _mediaStoppedAction?.Invoke();
                    _applicationService.EnableConstantDisplayAndPower(false);
                    OnStoppedMedia(new EventArgs());
                });

            CastCommand = new RelayCommand(async () =>
            {
                OnPausedMedia(new EventArgs());
                var message = new CastMediaMessage(MediaName, MediaPath, SubtitleFilePath);
                await Messenger.Default.SendAsync(message);
                if (message.Cancelled)
                {
                    OnResumedMedia(new EventArgs());
                    IsCasting = false;
                }
                else
                {
                    IsCasting = true;
                }
            });
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