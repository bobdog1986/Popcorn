using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        /// User Id (Machine Guid)
        /// </summary>
        private string UserId { get; }

        /// <summary>
        /// User
        /// </summary>
        private UserJson User { get; set; }

        private readonly SemaphoreSlim _semaphoreGet = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim _semaphoreUpdate = new SemaphoreSlim(1, 1);

        private bool _needSync = true;

        /// <summary>
        /// <see cref="IRestClient"/>
        /// </summary>
        private IRestClient RestClient { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="movieService"><see cref="IMovieService"/></param>
        /// <param name="showService"><see cref="IShowService"/></param>
        /// <param name="userId">User Id (Machine Guid)</param>
        public UserService(IMovieService movieService, IShowService showService, string userId)
        {
            ShowService = showService;
            MovieService = movieService;
            UserId = userId;
            RestClient = new RestClient(Constants.PopcornApi);
        }

        /// <summary>
        /// Refresh user history
        /// </summary>
        /// <returns></returns>
        private async Task GetHistoryAsync()
        {
            await _semaphoreGet.WaitAsync();
            try
            {
                if (!_needSync) return;
                var request = new RestRequest("/{segment}/{userId}", Method.GET);
                request.AddUrlSegment("segment", "user");
                request.AddUrlSegment("userId", UserId);
                var response = await RestClient.ExecuteTaskAsync<UserJson>(request);
                if (response.ErrorException != null)
                    throw response.ErrorException;
                User = response.Data;
                _needSync = false;
            }
            finally
            {
                _semaphoreGet.Release();
            }
        }

        /// <summary>
        /// Update user history
        /// </summary>
        /// <returns></returns>
        private async Task UpdateHistoryAsync()
        {
            await _semaphoreUpdate.WaitAsync();
            try
            {
                var request = new RestRequest("/{segment}", Method.POST);
                request.AddUrlSegment("segment", "user");
                request.AddJsonBody(User);
                var response = await RestClient.ExecuteTaskAsync<UserJson>(request);
                if (response.ErrorException != null)
                    throw response.ErrorException;
                User = response.Data;
                _needSync = true;
            }
            finally
            {
                _semaphoreUpdate.Release();
            }
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                    User.MovieHistory.Add(new MovieHistoryJson
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

                await UpdateHistoryAsync();
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
                    User.ShowHistory.Add(new ShowHistoryJson
                    {
                        ImdbId = show.ImdbId,
                        Favorite = show.IsFavorite,
                    });
                }
                else
                {
                    showToUpdate.Favorite = show.IsFavorite;
                }

                await UpdateHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
                User.DownloadLimit = limit;
                await UpdateHistoryAsync();
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
                await GetHistoryAsync();
                User.UploadLimit = limit;
                await UpdateHistoryAsync();
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
                await GetHistoryAsync();
                User.DefaultHdQuality = hd;
                await UpdateHistoryAsync();
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
                await GetHistoryAsync();
                User.DefaultSubtitleLanguage = englishName;
                await UpdateHistoryAsync();
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
                await GetHistoryAsync();
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
                await GetHistoryAsync();
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
        public ICollection<LanguageJson> GetAvailableLanguages()
        {
            var watch = Stopwatch.StartNew();
            ICollection<LanguageJson> availableLanguages = new List<LanguageJson>
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
        public async Task<LanguageJson> GetCurrentLanguageAsync()
        {
            try
            {
                LanguageJson currentLanguage = null;
                var watch = Stopwatch.StartNew();
                await GetHistoryAsync();
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
        public async Task SetCurrentLanguageAsync(LanguageJson language)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                User.Language.Culture = language.Culture;
                ChangeLanguage(User.Language);
                await GetHistoryAsync();
                await UpdateHistoryAsync();
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
        private void ChangeLanguage(LanguageJson language)
        {
            MovieService.ChangeTmdbLanguage(language);
            ShowService.ChangeTmdbLanguage(language);
            LocalizeDictionary.Instance.Culture = new CultureInfo(language.Culture);
            Messenger.Default.Send(new ChangeLanguageMessage());
        }
    }
}