using System.Collections.Generic;
using GalaSoft.MvvmLight;
using RestSharp.Deserializers;

namespace Popcorn.Models.Movie
{
    public class MovieLightJson : ObservableObject, IMovie
    {
        private string _posterImage;
        private string _genres;
        private bool _hasBeenSeen;
        private string _imdbCode;
        private bool _isFavorite;
        private double _rating;
        private string _title;
        private int _year;

        [DeserializeAs(Name = "imdb_code")]
        public string ImdbCode
        {
            get => _imdbCode;
            set { Set(() => ImdbCode, ref _imdbCode, value); }
        }

        [DeserializeAs(Name = "title")]
        public string Title
        {
            get => _title;
            set { Set(() => Title, ref _title, value); }
        }

        [DeserializeAs(Name = "year")]
        public int Year
        {
            get => _year;
            set { Set(() => Year, ref _year, value); }
        }

        [DeserializeAs(Name = "rating")]
        public double Rating
        {
            get => _rating;
            set { Set(() => Rating, ref _rating, value); }
        }

        [DeserializeAs(Name = "genres")]
        public string Genres
        {
            get => _genres;
            set { Set(() => Genres, ref _genres, value); }
        }

        [DeserializeAs(Name = "poster_image")]
        public string PosterImage
        {
            get => _posterImage;
            set { Set(() => PosterImage, ref _posterImage, value); }
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
    }
}
