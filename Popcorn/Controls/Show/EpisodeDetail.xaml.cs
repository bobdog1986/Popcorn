using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Converters;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using Popcorn.Models.Subtitles;
using Popcorn.Services.Subtitles;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.Controls.Show
{
    /// <summary>
    /// Logique d'interaction pour EpisodeDetail.xaml
    /// </summary>
    public partial class EpisodeDetail : INotifyPropertyChanged
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The subtitles service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// Play an episode
        /// </summary>
        private ICommand _playCommand;

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        private bool _loadingSubtitles;

        private double _torrentHealth;

        /// <summary>
        /// Selected episode
        /// </summary>
        public static readonly DependencyProperty EpisodeProperty =
            DependencyProperty.Register("Episode",
                typeof(EpisodeShowJson), typeof(EpisodeDetail),
                new PropertyMetadata(null, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var detail = dependencyObject as EpisodeDetail;
            var episode = detail?.Episode;
            if (episode == null) return;

            detail.Title.Text = episode.Title;
            detail.SeasonNumber.Text = $"Season {episode.Season}";
            detail.EpisodeNumber.Text = $"Episode {episode.EpisodeNumber}";
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var date = dtDateTime.AddSeconds(episode.FirstAired).ToLocalTime();
            detail.Duration.Text = $"Released {date.ToShortDateString()}";
            detail.Synopsis.Text = episode.Overview;
            var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
            episode.HdAvailable = episode.Torrents.Torrent_720p?.Url != null ||
                                  episode.Torrents.Torrent_1080p?.Url != null;
            episode.WatchHdQuality = episode.HdAvailable && applicationSettings.DefaultHdQuality;
            ComputeTorrentHealth(episode, detail);
            Task.Run(async () =>
            {
                await detail.LoadSubtitles(episode);
            });
        }

        /// <summary>
        /// Torrent health, from 0 to 10
        /// </summary>
        public double TorrentHealth
        {
            get => _torrentHealth;
            set
            {
                _torrentHealth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Compute torrent health
        /// </summary>
        private static void ComputeTorrentHealth(EpisodeShowJson episode, EpisodeDetail detail)
        {
            if (episode.SelectedTorrent == null) return;

            var torrent = episode.SelectedTorrent;
            if (torrent != null && torrent.Seeds < 4)
            {
                detail.TorrentHealth = 0;
            }
            else if (torrent != null && torrent.Seeds < 6)
            {
                detail.TorrentHealth = 1;
            }
            else if (torrent != null && torrent.Seeds < 8)
            {
                detail.TorrentHealth = 2;
            }
            else if (torrent != null && torrent.Seeds < 10)
            {
                detail.TorrentHealth = 3;
            }
            else if (torrent != null && torrent.Seeds < 12)
            {
                detail.TorrentHealth = 4;
            }
            else if (torrent != null && torrent.Seeds < 14)
            {
                detail.TorrentHealth = 5;
            }
            else if (torrent != null && torrent.Seeds < 16)
            {
                detail.TorrentHealth = 6;
            }
            else if (torrent != null && torrent.Seeds < 18)
            {
                detail.TorrentHealth = 7;
            }
            else if (torrent != null && torrent.Seeds < 20)
            {
                detail.TorrentHealth = 8;
            }
            else if (torrent != null && torrent.Seeds < 22)
            {
                detail.TorrentHealth = 9;
            }
            else if (torrent != null && torrent.Seeds >= 22)
            {
                detail.TorrentHealth = 10;
            }

            detail.Peers.Text = episode.SelectedTorrent.Peers.ToString();
            detail.Seeders.Text = episode.SelectedTorrent.Seeds.ToString();
            var brushConverter = new ValueToBrushConverter();
            var healthConverter = new TorrentHealthToLabelConverter();
            detail.Health.Foreground = (Brush)brushConverter.Convert(detail.TorrentHealth, typeof(TextBlock), "0|10",
                CultureInfo.CurrentCulture);
            detail.Health.Text = (string)healthConverter.Convert(detail.TorrentHealth, typeof(TextBlock), "0|10",
                CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        public bool LoadingSubtitles
        {
            get => _loadingSubtitles;
            set
            {
                _loadingSubtitles = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The selected episode
        /// </summary>
        public EpisodeShowJson Episode
        {
            get => (EpisodeShowJson) GetValue(EpisodeProperty);
            set => SetValue(EpisodeProperty, value);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EpisodeDetail()
        {
            InitializeComponent();
            _subtitlesService = SimpleIoc.Default.GetInstance<ISubtitlesService>();
            PlayCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new DownloadShowEpisodeMessage(Episode));
            });

            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, e =>
            {
                if (e.PropertyName != nameof(Episode.WatchHdQuality)) return;
                ComputeTorrentHealth(Episode, this);
            });
        }

        /// <summary>
        /// Play an episode
        /// </summary>
        public ICommand PlayCommand
        {
            get => _playCommand;
            set
            {
                _playCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Load the episode's subtitles asynchronously
        /// </summary>
        /// <param name="episode">The episode</param>
        private async Task LoadSubtitles(EpisodeShowJson episode)
        {
            Logger.Debug(
                $"Load subtitles for episode: {episode.Title}");
            LoadingSubtitles = true;
            try
            {
                var languages = (await _subtitlesService.GetSubLanguages()).ToList();
                if (int.TryParse(new string(episode.ImdbId
                    .SkipWhile(x => !char.IsDigit(x))
                    .TakeWhile(char.IsDigit)
                    .ToArray()), out int imdbId))
                {
                    var subtitles = await _subtitlesService.SearchSubtitlesFromImdb(
                            languages.Select(lang => lang.SubLanguageID).Aggregate((a, b) => a + "," + b),
                            imdbId.ToString(), episode.Season, episode.EpisodeNumber);

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        episode.AvailableSubtitles =
                            new ObservableCollection<Subtitle>(subtitles.OrderBy(a => a.LanguageName)
                                .Select(sub => new Subtitle
                                {
                                    Sub = sub
                                })
                                .GroupBy(x => x.Sub.LanguageName,
                                    (k, g) =>
                                        g.Aggregate(
                                            (a, x) =>
                                                (Convert.ToDouble(x.Sub.Rating, CultureInfo.InvariantCulture) >=
                                                 Convert.ToDouble(a.Sub.Rating, CultureInfo.InvariantCulture))
                                                    ? x
                                                    : a)));
                        episode.AvailableSubtitles.Insert(0, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"),
                                SubtitleId = "none"
                            }
                        });

                        episode.AvailableSubtitles.Insert(1, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("CustomLabel"),
                                SubtitleId = "custom"
                            }
                        });

                        var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                        if (!string.IsNullOrEmpty(applicationSettings.DefaultSubtitleLanguage) &&
                            episode.AvailableSubtitles.Any(
                                a => a.Sub.LanguageName == applicationSettings.DefaultSubtitleLanguage))
                        {
                            episode.SelectedSubtitle =
                                episode.AvailableSubtitles.FirstOrDefault(
                                    a => a.Sub.LanguageName == applicationSettings.DefaultSubtitleLanguage);
                        }
                        else
                        {
                            episode.SelectedSubtitle = episode.AvailableSubtitles.FirstOrDefault();
                        }
                    });

                    LoadingSubtitles = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed loading subtitles for : {episode.Title}. {ex.Message}");
                LoadingSubtitles = false;
            }
        }

        /// <summary>
        /// Implementation of <see cref="INotifyPropertyChanged"/>
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Event of <see cref="INotifyPropertyChanged"/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}