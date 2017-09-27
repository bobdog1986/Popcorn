using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Extensions;
using Popcorn.Messaging;
using Popcorn.Models.Cast;
using Popcorn.Models.Subtitles;
using Popcorn.Models.Torrent.Movie;
using RestSharp.Deserializers;
using Popcorn.Models.Media;

namespace Popcorn.Models.Movie
{
    public class MovieJson : ObservableObject, IMediaFile, IMovie
    {
        private List<CastJson> _cast;

        private ObservableCollection<Subtitle> _availableSubtitles =
            new ObservableCollection<Subtitle>();

        private int _imdbId;
        private string _dateUploaded;
        private string _posterImage;
        private string _backdropImage;
        private int _dateUploadedUnix;
        private string _descriptionFull;
        private string _descriptionIntro;
        private int _downloadCount;
        private string _filePath;
        private bool _fullHdAvailable;
        private List<string> _genres;
        private bool _hasBeenSeen;
        private string _backgroundImage;
        private string _largeCoverImage;
        private string _largeScreenshotImage1;
        private string _largeScreenshotImage2;
        private string _largeScreenshotImage3;
        private string _mediumCoverImage;
        private string _mediumScreenshotImage1;
        private string _mediumScreenshotImage2;
        private string _mediumScreenshotImage3;
        private string _smallCoverImage;
        private string _imdbCode;
        private bool _isFavorite;
        private string _language;
        private int _likeCount;
        private string _mpaRating;
        private double _rating;
        private int _runtime;
        private Subtitle _selectedSubtitle;
        private string _title;
        private string _titleLong;
        private string _slug;
        private List<TorrentJson> _torrents;
        private string _url;
        private bool _watchInFullHdQuality;
        private int _year;
        private string _ytTrailerCode;
        private List<string> _similars;

        [DataMember(Name = "url")]
        public string Url
        {
            get => _url;
            set { Set(() => Url, ref _url, value); }
        }

        [DataMember(Name = "imdb_code")]
        public string ImdbCode
        {
            get => _imdbCode;
            set { Set(() => ImdbCode, ref _imdbCode, value); }
        }

        [DataMember(Name = "title")]
        public string Title
        {
            get => _title;
            set { Set(() => Title, ref _title, value); }
        }

        [DataMember(Name = "title_long")]
        public string TitleLong
        {
            get => _titleLong;
            set { Set(() => TitleLong, ref _titleLong, value); }
        }

        [DataMember(Name = "slug")]
        public string Slug
        {
            get => _slug;
            set { Set(() => Slug, ref _slug, value); }
        }

        [DataMember(Name = "year")]
        public int Year
        {
            get => _year;
            set { Set(() => Year, ref _year, value); }
        }

        [DataMember(Name = "rating")]
        public double Rating
        {
            get => _rating;
            set { Set(() => Rating, ref _rating, value); }
        }

        [DataMember(Name = "runtime")]
        public int Runtime
        {
            get => _runtime;
            set { Set(() => Runtime, ref _runtime, value); }
        }

        [DataMember(Name = "genres")]
        public List<string> Genres
        {
            get => _genres;
            set { Set(() => Genres, ref _genres, value); }
        }

        [DataMember(Name = "language")]
        public string Language
        {
            get => _language;
            set { Set(() => Language, ref _language, value); }
        }

        [DataMember(Name = "mpa_rating")]
        public string MpaRating
        {
            get => _mpaRating;
            set { Set(() => MpaRating, ref _mpaRating, value); }
        }

        [DataMember(Name = "download_count")]
        public int DownloadCount
        {
            get => _downloadCount;
            set { Set(() => DownloadCount, ref _downloadCount, value); }
        }

        [DataMember(Name = "like_count")]
        public int LikeCount
        {
            get => _likeCount;
            set { Set(() => LikeCount, ref _likeCount, value); }
        }

        [DataMember(Name = "description_intro")]
        public string DescriptionIntro
        {
            get => _descriptionIntro;
            set { Set(() => DescriptionIntro, ref _descriptionIntro, value); }
        }

        [DataMember(Name = "description_full")]
        public string DescriptionFull
        {
            get => _descriptionFull;
            set { Set(() => DescriptionFull, ref _descriptionFull, value); }
        }

        [DataMember(Name = "yt_trailer_code")]
        public string YtTrailerCode
        {
            get => _ytTrailerCode;
            set { Set(() => YtTrailerCode, ref _ytTrailerCode, value); }
        }

        [DataMember(Name = "cast")]
        public List<CastJson> Cast
        {
            get => _cast;
            set { Set(() => Cast, ref _cast, value); }
        }

        [DataMember(Name = "torrents")]
        public List<TorrentJson> Torrents
        {
            get => _torrents;
            set { Set(() => Torrents, ref _torrents, value); }
        }

        [DataMember(Name = "date_uploaded")]
        public string DateUploaded
        {
            get => _dateUploaded;
            set { Set(() => DateUploaded, ref _dateUploaded, value); }
        }

        [DataMember(Name = "poster_image")]
        public string PosterImage
        {
            get => _posterImage;
            set { Set(() => PosterImage, ref _posterImage, value); }
        }

        [DataMember(Name = "backdrop_image")]
        public string BackdropImage
        {
            get => _backdropImage;
            set { Set(() => BackdropImage, ref _backdropImage, value); }
        }

        [DataMember(Name = "date_uploaded_unix")]
        public int DateUploadedUnix
        {
            get => _dateUploadedUnix;
            set { Set(() => DateUploadedUnix, ref _dateUploadedUnix, value); }
        }

        [DataMember(Name = "background_image")]
        public string BackgroundImage
        {
            get => _backgroundImage;
            set { Set(() => BackgroundImage, ref _backgroundImage, value); }
        }

        [DataMember(Name = "small_cover_image")]
        public string SmallCoverImage
        {
            get => _smallCoverImage;
            set { Set(() => SmallCoverImage, ref _smallCoverImage, value); }
        }

        [DataMember(Name = "medium_cover_image")]
        public string MediumCoverImage
        {
            get => _mediumCoverImage;
            set { Set(() => MediumCoverImage, ref _mediumCoverImage, value); }
        }

        [DataMember(Name = "large_cover_image")]
        public string LargeCoverImage
        {
            get => _largeCoverImage;
            set { Set(() => LargeCoverImage, ref _largeCoverImage, value); }
        }

        [DataMember(Name = "medium_screenshot_image1")]
        public string MediumScreenshotImage1
        {
            get => _mediumScreenshotImage1;
            set { Set(() => MediumScreenshotImage1, ref _mediumScreenshotImage1, value); }
        }

        [DataMember(Name = "medium_screenshot_image2")]
        public string MediumScreenshotImage2
        {
            get => _mediumScreenshotImage2;
            set { Set(() => MediumScreenshotImage2, ref _mediumScreenshotImage2, value); }
        }

        [DataMember(Name = "medium_screenshot_image3")]
        public string MediumScreenshotImage3
        {
            get => _mediumScreenshotImage3;
            set { Set(() => MediumScreenshotImage3, ref _mediumScreenshotImage3, value); }
        }

        [DataMember(Name = "large_screenshot_image1")]
        public string LargeScreenshotImage1
        {
            get => _largeScreenshotImage1;
            set { Set(() => LargeScreenshotImage1, ref _largeScreenshotImage1, value); }
        }

        [DataMember(Name = "large_screenshot_image2")]
        public string LargeScreenshotImage2
        {
            get => _largeScreenshotImage2;
            set { Set(() => LargeScreenshotImage2, ref _largeScreenshotImage2, value); }
        }

        [DataMember(Name = "large_screenshot_image3")]
        public string LargeScreenshotImage3
        {
            get => _largeScreenshotImage3;
            set { Set(() => LargeScreenshotImage3, ref _largeScreenshotImage3, value); }
        }

        [DataMember(Name = "similar")]
        public List<string> Similars
        {
            get => _similars;
            set { Set(() => Similars, ref _similars, value); }
        }

        /// <summary>
        /// Local path of the downloaded movie file
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set { Set(() => FilePath, ref _filePath, value); }
        }

        /// <summary>
        /// Decide if movie has to be watched in full HQ or not
        /// </summary>
        public bool WatchInFullHdQuality
        {
            get => _watchInFullHdQuality;
            set
            {
                var odlValue = _watchInFullHdQuality;
                Set(() => WatchInFullHdQuality, ref _watchInFullHdQuality, value);
                Messenger.Default.Send(new PropertyChangedMessage<bool>(this,
                    odlValue, value, nameof(WatchInFullHdQuality)));
            }
        }

        /// <summary>
        /// Indicate if full HQ quality is available
        /// </summary>
        public bool FullHdAvailable
        {
            get => _fullHdAvailable;
            set { Set(() => FullHdAvailable, ref _fullHdAvailable, value); }
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
                            _selectedSubtitle = AvailableSubtitles.FirstOrDefault(a => a.Sub.SubtitleId == "none");
                            RaisePropertyChanged();
                        }
                        else
                        {
                            SelectedSubtitle.FilePath = message.FileName;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Indicate if movie is favorite
        /// </summary>
        public bool IsFavorite
        {
            get => _isFavorite;
            set { Set(() => IsFavorite, ref _isFavorite, value); }
        }

        /// <summary>
        /// Indicate if movie has been seen by the user
        /// </summary>
        public bool HasBeenSeen
        {
            get => _hasBeenSeen;
            set { Set(() => HasBeenSeen, ref _hasBeenSeen, value); }
        }

        /// <summary>
        /// Imdb id
        /// </summary>
        public int ImdbId
        {
            get => _imdbId;
            set { Set(() => ImdbId, ref _imdbId, value); }
        }
    }
}