using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Popcorn.Models.Genres;
using Popcorn.Models.Movie;
using Popcorn.Models.User;

namespace Popcorn.Services.Movies.Movie
{
    public interface IMovieService
    {
        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        void ChangeTmdbLanguage(Language language);

        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        Task<MovieJson> GetMovieAsync(string imdbCode, CancellationToken ct);

        /// <summary>
        /// Get movie light by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        Task<MovieLightJson> GetMovieLightAsync(string imdbCode, CancellationToken ct);

        /// <summary>
        /// Get popular movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular movies and the number of movies found</returns>
        Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetMoviesAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null);

        /// <summary>
        /// Search movies by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched movies and the number of movies found</returns>
        Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct);

        /// <summary>
        /// Get similar movies
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="imdbIds">The imdbIds of the movies, split by comma</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Similar movies</returns>
        Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetSimilarAsync(int page,
            int limit,
            IEnumerable<string> imdbIds,
            CancellationToken ct);

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <returns>Task</returns>
        void TranslateMovie(IMovie movieToTranslate);

        /// <summary>
        /// Get the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        Task<string> GetMovieTrailerAsync(MovieJson movie, CancellationToken ct);

        /// <summary>
        /// Get movies similar async
        /// </summary>
        /// <param name="movie">Movie</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>Movies</returns>
        Task<IEnumerable<MovieLightJson>> GetMoviesSimilarAsync(MovieJson movie, CancellationToken ct);
    }
}