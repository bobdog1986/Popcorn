using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Extensions;
using Popcorn.Messaging;
using Popcorn.Models.Subtitles;
using Popcorn.Models.Torrent.Show;
using RestSharp.Deserializers;
using Popcorn.Models.Media;

namespace Popcorn.Models.Episode
{
    public class EpisodeShowJson : ObservableObject, IMediaFile
    {
        private bool _watchHdQuality;

        private string _title;

        private string _filePath;

        private string _imdbId;

        private ObservableCollection<Subtitle> _availableSubtitles =
            new ObservableCollection<Subtitle>();

        private Subtitle _selectedSubtitle;

        private TorrentShowNodeJson _torrents;

        private long _firstAired;

        private bool _dateBased;

        private string _overview;

        private int _episodeNumber;

        private int _season;

        private int? _tvdbId;

        private bool _hdAvailable;

        private TorrentShowJson _selectedTorrent;

        public bool WatchHdQuality
        {
            get => _watchHdQuality;
            set
            {
                var odlValue = _watchHdQuality;
                Set(ref _watchHdQuality, value);
                if (Torrents == null) return;

                if (value && (Torrents.Torrent_720p?.Url != null ||
                              Torrents.Torrent_1080p?.Url != null))
                {
                    SelectedTorrent = !string.IsNullOrEmpty(Torrents.Torrent_1080p?.Url)
                        ? Torrents.Torrent_1080p
                        : Torrents.Torrent_720p;
                }
                else
                {
                    SelectedTorrent = !string.IsNullOrEmpty(Torrents.Torrent_480p?.Url)
                        ? Torrents.Torrent_480p
                        : Torrents.Torrent_0;
                }

                Messenger.Default.Send(new PropertyChangedMessage<bool>(this,
                    odlValue, value, nameof(WatchHdQuality)));
            }
        }

        /// <summary>
        /// Indicate if full HQ quality is available
        /// </summary>
        public bool HdAvailable
        {
            get => _hdAvailable;
            set { Set(() => HdAvailable, ref _hdAvailable, value); }
        }

        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value);
        }
        
        public TorrentShowJson SelectedTorrent
        {
            get => _selectedTorrent;
            set => Set(ref _selectedTorrent, value);
        }

        public string ImdbId
        {
            get => _imdbId;
            set => Set(ref _imdbId, value);
        }

        /// <summary>
        /// Available subtitles
        /// </summary>
        public ObservableCollection<Subtitle> AvailableSubtitles
        {
            get => _availableSubtitles;
            set { Set(() => AvailableSubtitles, ref _availableSubtitles, value); }
        }

        /// <summary>
        /// Selected subtitle
        /// </summary>
        public Subtitle SelectedSubtitle
        {
            get => _selectedSubtitle;
            set
            {
                Set(() => SelectedSubtitle, ref _selectedSubtitle, value);
                if (SelectedSubtitle != null && SelectedSubtitle.Sub.SubtitleId == "custom")
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                    {
                        var message = new CustomSubtitleMessage();
                        await Messenger.Default.SendAsync(message);
                        if (message.Error || string.IsNullOrEmpty(message.FileName))
                        {
                            SelectedSubtitle.FilePath = string.Empty;
                        }
                        else
                        {
                            SelectedSubtitle.FilePath = message.FileName;
                        }
                    });
                }
            }
        }

        [DataMember(Name = "torrents")]
        public TorrentShowNodeJson Torrents
        {
            get => _torrents;
            set => Set(ref _torrents, value);
        }

        [DataMember(Name = "first_aired")]
        public long FirstAired
        {
            get => _firstAired;
            set => Set(ref _firstAired, value);
        }

        [DataMember(Name = "date_based")]
        public bool DateBased
        {
            get => _dateBased;
            set => Set(ref _dateBased, value);
        }

        [DataMember(Name = "overview")]
        public string Overview
        {
            get => _overview;
            set => Set(ref _overview, value);
        }

        [DataMember(Name = "title")]
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        [DataMember(Name = "episode")]
        public int EpisodeNumber
        {
            get => _episodeNumber;
            set => Set(ref _episodeNumber, value);
        }

        [DataMember(Name = "season")]
        public int Season
        {
            get => _season;
            set => Set(ref _season, value);
        }

        [DataMember(Name = "tvdb_id")]
        public int? TvdbId
        {
            get => _tvdbId;
            set => Set(ref _tvdbId, value);
        }
    }
}