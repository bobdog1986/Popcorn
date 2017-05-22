using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using Popcorn.Models.Episode;
using Popcorn.Models.Image;
using Popcorn.Models.Rating;
using RestSharp.Deserializers;

namespace Popcorn.Models.Shows
{
    public class ShowJson : ObservableObject
    {
        private bool _isFavorite;

        private string _imdbId;

        private string _tvdbId;

        private string _title;

        private int _year;

        private string _slug;

        private string _synopsis;

        private string _runtime;

        private string _country;

        private string _network;

        private string _airDay;

        private string _airTime;

        private string _status;

        private int _numSeasons;

        private long _lastUpdated;

        private List<EpisodeShowJson> _episodes;

        private List<string> _genres;

        private ImageShowJson _images;

        private RatingJson _rating;

        private List<string> _similars;

        [DeserializeAs(Name = "imdb_id")]
        public string ImdbId
        {
            get => _imdbId;
            set => Set(ref _imdbId, value);
        }

        [DeserializeAs(Name = "tvdb_id")]
        public string TvdbId
        {
            get => _tvdbId;
            set => Set(ref _tvdbId, value);
        }

        [DeserializeAs(Name = "title")]
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        [DeserializeAs(Name = "year")]
        public int Year
        {
            get => _year;
            set => Set(ref _year, value);
        }

        [DeserializeAs(Name = "slug")]
        public string Slug
        {
            get => _slug;
            set => Set(ref _slug, value);
        }

        [DeserializeAs(Name = "synopsis")]
        public string Synopsis
        {
            get => _synopsis;
            set => Set(ref _synopsis, value);
        }

        [DeserializeAs(Name = "runtime")]
        public string Runtime
        {
            get => _runtime;
            set => Set(ref _runtime, value);
        }

        [DeserializeAs(Name = "country")]
        public string Country
        {
            get => _country;
            set => Set(ref _country, value);
        }

        [DeserializeAs(Name = "network")]
        public string Network
        {
            get => _network;
            set => Set(ref _network, value);
        }

        [DeserializeAs(Name = "air_day")]
        public string AirDay
        {
            get => _airDay;
            set => Set(ref _airDay, value);
        }

        [DeserializeAs(Name = "air_time")]
        public string AirTime
        {
            get => _airTime;
            set => Set(ref _airTime, value);
        }

        [DeserializeAs(Name = "status")]
        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        [DeserializeAs(Name = "num_seasons")]
        public int NumSeasons
        {
            get => _numSeasons;
            set => Set(ref _numSeasons, value);
        }

        [DeserializeAs(Name = "last_updated")]
        public long LastUpdated
        {
            get => _lastUpdated;
            set => Set(ref _lastUpdated, value);
        }

        [DeserializeAs(Name = "episodes")]
        public List<EpisodeShowJson> Episodes
        {
            get => _episodes;
            set => Set(ref _episodes, value);
        }

        [DeserializeAs(Name = "genres")]
        public List<string> Genres
        {
            get => _genres;
            set => Set(ref _genres, value);
        }

        [DeserializeAs(Name = "images")]
        public ImageShowJson Images
        {
            get => _images;
            set => Set(ref _images, value);
        }

        [DeserializeAs(Name = "rating")]
        public RatingJson Rating
        {
            get => _rating;
            set => Set(ref _rating, value);
        }

        [DeserializeAs(Name = "similar")]
        public List<string> Similars
        {
            get => _similars;
            set => Set(ref _similars, value);
        }

        /// <summary>
        /// Indicate if movie is favorite
        /// </summary>
        public bool IsFavorite
        {
            get => _isFavorite;
            set { Set(() => IsFavorite, ref _isFavorite, value); }
        }
    }
}
