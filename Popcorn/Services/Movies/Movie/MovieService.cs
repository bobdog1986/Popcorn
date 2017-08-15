using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Movie;
using RestSharp;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using System.Collections.Async;
using GalaSoft.MvvmLight.Ioc;
using Popcorn.Models.Genres;
using Popcorn.Models.Trailer;
using Popcorn.Models.User;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Windows.Settings;
using Popcorn.YTVideoProvider;

namespace Popcorn.Services.Movies.Movie
{
    /// <summary>
    /// Services used to interact with movies
    /// </summary>
    public class MovieService : IMovieService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initialize a new instance of MovieService class
        /// </summary>
        public MovieService()
        {
            TmdbClient = new TMDbClient(Utils.Constants.TmDbClientId, true)
            {
                MaxRetryCount = 50
            };

            try
            {
                TmdbClient.GetConfig();
            }
            catch (Exception)
            {
                // An issue occured with TmdbClient
            }
        }

        /// <summary>
        /// TMDb client
        /// </summary>
        private TMDbClient TmdbClient { get; }

        /// <summary>
        /// True if movie languages must be refreshed
        /// </summary>
        private bool MustRefreshLanguage { get; set; }

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public void ChangeTmdbLanguage(LanguageJson language)
        {
            if (TmdbClient.DefaultLanguage == null)
            {
                MustRefreshLanguage = false;
            }
            else
            {
                MustRefreshLanguage = TmdbClient.DefaultLanguage != language.Culture;
            }

            TmdbClient.DefaultLanguage = language.Culture;
        }

        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <returns>The movie</returns>
        public async Task<MovieJson> GetMovieAsync(string imdbCode)
        {
            var watch = Stopwatch.StartNew();

            var restClient = new RestClient(Utils.Constants.PopcornApi)
            {
                Timeout = 5000
            };
            var request = new RestRequest("/{segment}/{movie}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddUrlSegment("movie", imdbCode);
            var movie = new MovieJson();

            try
            {
                var response = await restClient.ExecuteTaskAsync<MovieJson>(request);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                movie = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieAsync ({imdbCode}) in {elapsedMs} milliseconds.");
            }

            return movie;
        }

        /// <summary>
        /// Get movies similar async
        /// </summary>
        /// <param name="movie">Movie</param>
        /// <returns>Movies</returns>
        public async Task<List<MovieJson>> GetMoviesSimilarAsync(MovieJson movie)
        {
            var watch = Stopwatch.StartNew();
            var movies = new List<MovieJson>();
            try
            {
                if (movie.Similars != null && movie.Similars.Any())
                {
                    await movie.Similars.ParallelForEachAsync(async imdbCode =>
                    {
                        try
                        {
                            var similar = await GetMovieAsync(imdbCode);
                            if (similar != null)
                            {
                                movies.Add(similar);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMoviesSimilarAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMoviesSimilarAsync in {elapsedMs} milliseconds.");
            }

            return movies;
        }

        /// <summary>
        /// Get movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> GetMoviesAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var watch = Stopwatch.StartNew();
            var wrapper = new MovieResponse();
            if (limit < 1 || limit > 50)
                limit = Utils.Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Utils.Constants.PopcornApi)
            {
                Timeout = 5000
            };
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("sort_by", sortBy);
            try
            {
                var response = await restClient.ExecuteTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMoviesAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Task.Run(async () =>
            {
                await ProcessTranslations(result).ConfigureAwait(false);
            }).ConfigureAwait(false);

            var nbResult = wrapper?.TotalMovies ?? 0;
            return (result, nbResult);
        }

        /// <summary>
        /// Process translations for a list of movies
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task ProcessTranslations(IEnumerable<MovieJson> result)
        {
            await result.ParallelForEachAsync(async movie =>
            {
                await TranslateMovieAsync(movie);
            });
        }

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
        public async Task<(IEnumerable<MovieJson> movies, int nbMovies)> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            var wrapper = new MovieResponse();
            if (limit < 1 || limit > 50)
                limit = Utils.Constants.MaxMoviesPerPage;

            if (page < 1)
                page = 1;

            var restClient = new RestClient(Utils.Constants.PopcornApi)
            {
                Timeout = 5000
            };
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movies");
            request.AddParameter("limit", limit);
            request.AddParameter("page", page);
            if (genre != null) request.AddParameter("genre", genre.EnglishName);
            request.AddParameter("minimum_rating", ratingFilter);
            request.AddParameter("query_term", criteria);

            try
            {
                var response = await restClient.ExecuteTaskAsync<MovieResponse>(request, ct);
                if (response.ErrorException != null)
                    throw response.ErrorException;

                wrapper = response.Data;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "SearchMoviesAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"SearchMoviesAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"SearchMoviesAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
            }

            var result = wrapper?.Movies ?? new List<MovieJson>();
            Task.Run(async () =>
            {
                await ProcessTranslations(result).ConfigureAwait(false);
            }).ConfigureAwait(false);

            var nbResult = wrapper?.TotalMovies ?? 0;
            return (result, nbResult);
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <returns>Task</returns>
        public async Task TranslateMovieAsync(MovieJson movieToTranslate)
        {
            if (!MustRefreshLanguage) return;
            var watch = Stopwatch.StartNew();
            try
            {
                var movie = await TmdbClient.GetMovieAsync(movieToTranslate.ImdbCode,
                    MovieMethods.Credits);
                movieToTranslate.Title = movie?.Title;
                movieToTranslate.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                movieToTranslate.DescriptionFull = movie?.Overview;
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "TranslateMovieAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"TranslateMovieAsync: {exception.Message}");
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"TranslateMovieAsync ({movieToTranslate.ImdbCode}) in {elapsedMs} milliseconds.");
            }
        }

        /// <summary>
        /// Get the link to the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<string> GetMovieTrailerAsync(MovieJson movie, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            var uri = string.Empty;
            try
            {
                var tmdbMovie = await TmdbClient.GetMovieAsync(movie.ImdbCode, MovieMethods.Videos);
                var trailers = tmdbMovie?.Videos;
                if (trailers != null && trailers.Results.Any())
                {
                    using (var service = Client.For(YouTube.Default))
                    {
                        var videos =
                            (await service.GetAllVideosAsync("https://youtube.com/watch?v=" + trailers.Results
                                                                 .FirstOrDefault()
                                                                 .Key))
                            .ToList();
                        if (videos.Any())
                        {
                            var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                            var maxRes = settings.DefaultHdQuality ? 1080 : 720;
                            uri =
                                await videos.Where(a => !a.Is3D && a.Resolution <= maxRes &&
                                                        a.Format == VideoFormat.Mp4 && a.AudioBitrate > 0)
                                    .Aggregate((i1, i2) => i1.Resolution > i2.Resolution ? i1 : i2).GetUriAsync();
                        }
                    }
                }
                else
                {
                    throw new PopcornException("No trailer found.");
                }
            }
            catch (Exception exception) when (exception is TaskCanceledException)
            {
                Logger.Debug(
                    "GetMovieTrailerAsync cancelled.");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"GetMovieTrailerAsync: {exception.Message}");
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug(
                    $"GetMovieTrailerAsync ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
            }

            return uri;
        }
    }
}