using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akavache;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Localization;
using Popcorn.Models.Movie;
using Popcorn.Models.Shows;
using Popcorn.Models.User;
using Popcorn.Services.Movies.Movie;
using Popcorn.Utils;
using RestSharp;
using WPFLocalizeExtension.Engine;
using Popcorn.Services.Shows.Show;
using Language = Popcorn.Models.User.Language;

namespace Popcorn.Services.User
{
    /// <summary>
    /// Services used to interact with user history
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Services used to interact with movies
        /// </summary>
        private IMovieService MovieService { get; }

        /// <summary>
        /// Services used to interact with shows
        /// </summary>
        private IShowService ShowService { get; }

        /// <summary>
        /// User
        /// </summary>
        private Models.User.User User { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="movieService"><see cref="IMovieService"/></param>
        /// <param name="showService"><see cref="IShowService"/></param>
        public UserService(IMovieService movieService, IShowService showService)
        {
            ShowService = showService;
            MovieService = movieService;
        }

        private async Task<Models.User.User> GetUser()
        {
            var user = new Models.User.User
            {
                Language = new Language()
            };

            try
            {
                user = await BlobCache.UserAccount.GetObject<Models.User.User>("user");
                if (user.Language == null)
                {
                    user.Language = new Language();
                }

                if (user.MovieHistory == null)
                {
                    user.MovieHistory = new List<MovieHistory>();
                }

                if (user.ShowHistory == null)
                {
                    user.ShowHistory = new List<ShowHistory>();
                }
            }
            catch (Exception)
            {

            }

            return user;
        }

        private async Task UpdateUser(Models.User.User user)
        {
            await BlobCache.UserAccount.InsertObject("user", user);
            await BlobCache.UserAccount.Flush();
        }

        /// <summary>
        /// Set if movies have been seen or set as favorite
        /// </summary>
        /// <param name="movies">All movies to compute</param>
        public async Task SyncMovieHistoryAsync(IEnumerable<IMovie> movies)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                User = await GetUser().ConfigureAwait(false);
                foreach (var movie in movies)
                {
                    var updatedMovie = User.MovieHistory.FirstOrDefault(p => p.ImdbId == movie.ImdbCode);
                    if (updatedMovie == null) continue;
                    movie.IsFavorite = updatedMovie.Favorite;
                    movie.HasBeenSeen = updatedMovie.Seen;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SyncMovieHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SyncMovieHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set if shows have been seen or set as favorite
        /// </summary>
        /// <param name="shows">All shows to compute</param>
        public async Task SyncShowHistoryAsync(IEnumerable<IShow> shows)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                User = await GetUser().ConfigureAwait(false);
                foreach (var show in shows)
                {
                    var updatedShow = User.ShowHistory.FirstOrDefault(p => p.ImdbId == show.ImdbId);
                    if (updatedShow == null) continue;
                    show.IsFavorite = updatedShow.Favorite;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SyncShowHistoryAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SyncShowHistoryAsync in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set the movie
        /// </summary>
        /// <param name="movie">Movie</param>
        public async Task SetMovieAsync(IMovie movie)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var movieToUpdate = User.MovieHistory.FirstOrDefault(a => a.ImdbId == movie.ImdbCode);
                if (movieToUpdate == null)
                {
                    User.MovieHistory.Add(new MovieHistory
                    {
                        ImdbId = movie.ImdbCode,
                        Favorite = movie.IsFavorite,
                        Seen = movie.HasBeenSeen
                    });
                }
                else
                {
                    movieToUpdate.Seen = movie.HasBeenSeen;
                    movieToUpdate.Favorite = movie.IsFavorite;
                }

                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetMovieAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Set the show
        /// </summary>
        /// <param name="show">Show</param>
        public async Task SetShowAsync(IShow show)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var showToUpdate = User.ShowHistory.FirstOrDefault(a => a.ImdbId == show.ImdbId);
                if (showToUpdate == null)
                {
                    User.ShowHistory.Add(new ShowHistory
                    {
                        ImdbId = show.ImdbId,
                        Favorite = show.IsFavorite,
                    });
                }
                else
                {
                    showToUpdate.Favorite = show.IsFavorite;
                }

                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SetShowAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetShowAsync ({show.ImdbId}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get seen movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)>
            GetSeenMovies(int page)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                var movies = User.MovieHistory.Where(a => a.Seen).Select(a => a.ImdbId).ToList();
                var skip = (page - 1) * Constants.MaxMoviesPerPage;
                if (movies.Count <= Constants.MaxMoviesPerPage)
                {
                    skip = 0;
                }

                return (movies.Skip(skip).Take(Constants.MaxMoviesPerPage), movies, movies.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<string>(), new List<string>(), 0);
            }
        }

        /// <summary>
        /// Get seen shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)> GetSeenShows(int page)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                var shows = User.ShowHistory.Where(a => a.Seen).Select(a => a.ImdbId).ToList();
                var skip = (page - 1) * Constants.MaxShowsPerPage;
                if (shows.Count <= Constants.MaxShowsPerPage)
                {
                    skip = 0;
                }

                return (shows.Skip(skip).Take(Constants.MaxShowsPerPage), shows, shows.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<string>(), new List<string>(), 0);
            }
        }

        /// <summary>
        /// Get favorites movies
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> movies, IEnumerable<string> allMovies, int nbMovies)>
            GetFavoritesMovies(int page)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                var movies = User.MovieHistory.Where(a => a.Favorite).Select(a => a.ImdbId).ToList();
                var skip = (page - 1) * Constants.MaxMoviesPerPage;
                if (movies.Count <= Constants.MaxMoviesPerPage)
                {
                    skip = 0;
                }

                return (movies.Skip(skip).Take(Constants.MaxMoviesPerPage), movies, movies.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<string>(), new List<string>(), 0);
            }
        }

        /// <summary>
        /// Get favorites shows
        /// </summary>
        /// <param name="page">Pagination</param>
        /// <returns>List of ImdbId</returns>
        public async Task<(IEnumerable<string> shows, IEnumerable<string> allShows, int nbShows)>
            GetFavoritesShows(int page)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                var shows = User.ShowHistory.Where(a => a.Favorite).Select(a => a.ImdbId).ToList();
                var skip = (page - 1) * Constants.MaxShowsPerPage;
                if (shows.Count <= Constants.MaxShowsPerPage)
                {
                    skip = 0;
                }

                return (shows.Skip(skip).Take(Constants.MaxShowsPerPage), shows, shows.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<string>(), new List<string>(), 0);
            }
        }

        /// <summary>
        /// Get the download rate
        /// </summary>
        /// <returns>Download rate</returns>
        public async Task<int> GetDownloadLimit()
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                return User.DownloadLimit;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 0;
            }
        }

        /// <summary>
        /// Get the upload rate
        /// </summary>
        /// <returns>Upload rate</returns>
        public async Task<int> GetUploadLimit()
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                return User.UploadLimit;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 0;
            }
        }

        /// <summary>
        /// Set the download rate
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task SetDownloadLimit(int limit)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                User.DownloadLimit = limit;
                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Set the upload rate
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task SetUploadLimit(int limit)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                User.UploadLimit = limit;
                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Set default HD quality
        /// </summary>
        /// <param name="hd"></param>
        /// <returns></returns>
        public async Task SetDefaultHdQuality(bool hd)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                User.DefaultHdQuality = hd;
                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Set default subtitle language
        /// </summary>
        /// <param name="englishName"></param>
        /// <returns></returns>
        public async Task SetDefaultSubtitleLanguage(string englishName)
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                User.DefaultSubtitleLanguage = englishName;
                await UpdateUser(User).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Set default HD quality
        /// </summary>
        /// <returns></returns>

        public async Task<bool> GetDefaultHdQuality()
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                return User.DefaultHdQuality;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Get default subtitle language
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDefaultSubtitleLanguage()
        {
            try
            {
                User = await GetUser().ConfigureAwait(false);
                return User.DefaultSubtitleLanguage;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return "en";
            }
        }

        /// <summary>
        /// Get all available languages from the database
        /// </summary>
        /// <returns>All available languages</returns>
        public ICollection<Language> GetAvailableLanguages()
        {
            var watch = Stopwatch.StartNew();
            ICollection<Language> availableLanguages = new List<Language>
            {
                new EnglishLanguage(),
                new FrenchLanguage(),
                new SpanishLanguage()
            };
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug(
                $"GetAvailableLanguages in {elapsedMs} milliseconds.");

            return availableLanguages;
        }

        /// <summary>
        /// Get the current language of the application
        /// </summary>
        /// <returns>Current language</returns>
        public async Task<Language> GetCurrentLanguageAsync()
        {
            try
            {
                Language currentLanguage;
                var watch = Stopwatch.StartNew();
                User = await GetUser().ConfigureAwait(false);
                var language = User.Language;
                if (language != null)
                {
                    switch (language.Culture)
                    {
                        case "fr":
                            currentLanguage = new FrenchLanguage();
                            break;
                        case "es":
                            currentLanguage = new SpanishLanguage();
                            break;
                        default:
                            currentLanguage = new EnglishLanguage();
                            break;
                    }
                }
                else
                {
                    currentLanguage = new EnglishLanguage();
                }

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetCurrentLanguageAsync in {elapsedMs} milliseconds.");

                return currentLanguage;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new EnglishLanguage();
            }
        }

        /// <summary>
        /// Set the current language of the application
        /// </summary>
        /// <param name="language">Language</param>
        public async Task SetCurrentLanguageAsync(Language language)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                User = await GetUser().ConfigureAwait(false);
                User.Language.Culture = language.Culture;
                ChangeLanguage(User.Language);
                await UpdateUser(User).ConfigureAwait(false);
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SetCurrentLanguageAsync ({User.Language.Name}) in {elapsedMs} milliseconds.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Change language
        /// </summary>
        /// <param name="language"></param>
        private void ChangeLanguage(Language language)
        {
            MovieService.ChangeTmdbLanguage(language);
            ShowService.ChangeTmdbLanguage(language);
            LocalizeDictionary.Instance.Culture = new CultureInfo(language.Culture);
            Messenger.Default.Send(new ChangeLanguageMessage());
        }
    }
}