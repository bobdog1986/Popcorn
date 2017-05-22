using System.Collections.Generic;
using System.Threading.Tasks;
using Popcorn.Models.Episode;
using Popcorn.Models.Movie;
using Popcorn.Models.Shows;
using Popcorn.Models.User;

namespace Popcorn.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// Get all available languages
        /// </summary>
        /// <returns>All available languages</returns>
        ICollection<LanguageJson> GetAvailableLanguages();

        /// <summary>
        /// Get the current language of the application
        /// </summary>
        Task<LanguageJson> GetCurrentLanguageAsync();

        /// <summary>
        /// Set the current language of the application
        /// </summary>
        /// <param name="language">Language</param>
        Task SetCurrentLanguageAsync(LanguageJson language);

        /// <summary>
        /// Set if movies have been seen or set as favorite
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        Task SyncMovieHistoryAsync(IEnumerable<MovieJson> movies);

        /// <summary>
        /// Set if movies have been seen or set as favorite
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        Task SyncShowHistoryAsync(IEnumerable<ShowJson> movies);

        /// <summary>
        /// Set the movie
        /// </summary>
        /// <param name="movie">Favorite movie</param>
        Task SetMovieAsync(MovieJson movie);

        /// <summary>
        /// Set the show
        /// </summary>
        /// <param name="show">Show</param>
        Task SetShowAsync(ShowJson show);

        /// <summary>
        /// Get seen movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)> GetSeenMovies(int page);

        /// <summary>
        /// Get seen shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)> GetSeenShows(int page);

        /// <summary>
        /// Get favorites movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)> GetFavoritesMovies(int page);

        /// <summary>
        /// Get favorites shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)> GetFavoritesShows(int page);

        /// <summary>
        /// Get the download limit
        /// </summary>
        /// <returns>Download rate</returns>
        Task<int> GetDownloadLimit();

        /// <summary>
        /// Get the upload limit
        /// </summary>
        /// <returns>Upload rate</returns>
        Task<int> GetUploadLimit();

        /// <summary>
        /// Set the download limit
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        Task SetDownloadLimit(int limit);

        /// <summary>
        /// Set the upload limit
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        Task SetUploadLimit(int limit);

        /// <summary>
        /// Set default HD quality
        /// </summary>
        /// <param name="hd"></param>
        /// <returns></returns>
        Task SetDefaultHdQuality(bool hd);

        /// <summary>
        /// Set default subtitle language
        /// </summary>
        /// <param name="englishName"></param>
        /// <returns></returns>
        Task SetDefaultSubtitleLanguage(string englishName);

        /// <summary>
        /// Set default HD quality
        /// </summary>
        /// <returns></returns>
        Task<bool> GetDefaultHdQuality();

        /// <summary>
        /// Set default subtitle language
        /// </summary>
        /// <returns></returns>
        Task<string> GetDefaultSubtitleLanguage();
    }
}