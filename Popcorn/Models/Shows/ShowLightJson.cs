using GalaSoft.MvvmLight;
using Popcorn.Models.Image;
using Popcorn.Models.Rating;
using RestSharp.Deserializers;

namespace Popcorn.Models.Shows
{
    public class ShowLightJson : ObservableObject, IShow
    {
        private bool _isFavorite;

        private string _imdbId;

        private string _tvdbId;

        private string _title;

        private int _year;

        private string _genres;

        private ImageShowJson _images;

        private RatingJson _rating;

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
            set
            {
                var newTitle = value.Replace("&amp;", "&");
                Set(ref _title, newTitle);
            }
        }

        [DeserializeAs(Name = "year")]
        public int Year
        {
            get => _year;
            set => Set(ref _year, value);
        }

        [DeserializeAs(Name = "genres")]
        public string Genres
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
