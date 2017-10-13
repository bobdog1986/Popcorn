using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Movie;
using RestSharp;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using GalaSoft.MvvmLight.Ioc;
using Popcorn.Models.Genres;
using Popcorn.Models.User;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Windows.Settings;
using Popcorn.YTVideoProvider;
using Polly;
using Polly.Timeout;
using Popcorn.Extensions;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.General;
using TMDbLib.Objects.People;
using Utf8Json;

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
        /// Movies to translate
        /// </summary>
        private readonly Subject<IMovie> _moviesToTranslateObservable;

        /// <summary>
        /// Initialize a new instance of MovieService class
        /// </summary>
        public MovieService()
        {
            _moviesToTranslateObservable = new Subject<IMovie>();
            TmdbClient = new TMDbClient(Utils.Constants.TmDbClientId, true)
            {
                MaxRetryCount = 50
            };

            Task.Run(() =>
            {
                try
                {
                    TmdbClient.GetConfig();
                    _moviesToTranslateObservable.Drain(s => Observable.Return(s).Delay(TimeSpan.FromMilliseconds(250)))
                        .Subscribe(async movieToTranslate =>
                        {
                            var timeBeforeTimeOut = 3;
                            var timeoutPolicy =
                                Policy.TimeoutAsync(timeBeforeTimeOut, TimeoutStrategy.Pessimistic);
                            try
                            {
                                await timeoutPolicy.ExecuteAsync(async () =>
                                {
                                    try
                                    {
                                        var movie = await TmdbClient.GetMovieAsync(movieToTranslate.ImdbCode,
                                            MovieMethods.Credits).ConfigureAwait(false);
                                        if (movieToTranslate is MovieJson refMovie)
                                        {
                                            refMovie.TranslationLanguage = TmdbClient.DefaultLanguage;
                                            refMovie.Title = movie?.Title;
                                            refMovie.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                                            refMovie.DescriptionFull = movie?.Overview;
                                        }
                                        else if (movieToTranslate is MovieLightJson refMovieLight)
                                        {
                                            refMovieLight.TranslationLanguage = TmdbClient.DefaultLanguage;
                                            refMovieLight.Title = movie?.Title;
                                            refMovieLight.Genres = movie?.Genres != null
                                                ? string.Join(", ", movie.Genres?.Select(a => a.Name))
                                                : string.Empty;
                                        }
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
                                }).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn(
                                    $"Movie {movieToTranslate.ImdbCode} has not been translated in {timeBeforeTimeOut} seconds. Error {ex.Message}");
                            }
                        });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        /// <summary>
        /// TMDb client
        /// </summary>
        private TMDbClient TmdbClient { get; }

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public void ChangeTmdbLanguage(Language language)
        {
            TmdbClient.DefaultLanguage = language.Culture;
        }

        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        public async Task<MovieJson> GetMovieAsync(string imdbCode, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}/{movie}", Method.GET);
                    request.AddUrlSegment("segment", "movies");
                    request.AddUrlSegment("movie", imdbCode);
                    var movie = new MovieJson();

                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        movie = JsonSerializer.Deserialize<MovieJson>(response.RawBytes);
                        movie.TranslationLanguage = TmdbClient.DefaultLanguage;
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
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get light movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        public async Task<MovieLightJson> GetMovieLightAsync(string imdbCode, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}/light/{movie}", Method.GET);
                    request.AddUrlSegment("segment", "movies");
                    request.AddUrlSegment("movie", imdbCode);
                    var movie = new MovieLightJson();

                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        movie = JsonSerializer.Deserialize<MovieLightJson>(response.RawBytes);
                        movie.TranslationLanguage = TmdbClient.DefaultLanguage;
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieLightAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieLightAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetMovieLightAsync ({imdbCode}) in {elapsedMs} milliseconds.");
                    }

                    return movie;
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get movies similar async
        /// </summary>
        /// <param name="movie">Movie</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>Movies</returns>
        public async Task<IEnumerable<MovieLightJson>> GetMoviesSimilarAsync(MovieJson movie, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    (IEnumerable<MovieLightJson> movies, int nbMovies) similarMovies = (new List<MovieLightJson>(), 0);
                    try
                    {
                        if (movie.Similars != null && movie.Similars.Any())
                        {
                            similarMovies = await GetSimilarAsync(0, Utils.Constants.MaxMoviesPerPage, movie.Similars,
                                    CancellationToken.None)
                                .ConfigureAwait(false);
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

                    return similarMovies.movies.Where(
                        a => a.ImdbCode != movie.ImdbCode);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new List<MovieLightJson>();
            }
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
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetMoviesAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new MovieLightResponse();
                    if (limit < 1 || limit > 50)
                        limit = Utils.Constants.MaxMoviesPerPage;

                    if (page < 1)
                        page = 1;

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}", Method.GET);
                    request.AddUrlSegment("segment", "movies");
                    request.AddParameter("limit", limit);
                    request.AddParameter("page", page);
                    if (genre != null) request.AddParameter("genre", genre.EnglishName);
                    request.AddParameter("minimum_rating", Convert.ToInt32(ratingFilter));
                    request.AddParameter("sort_by", sortBy);
                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        wrapper = JsonSerializer.Deserialize<MovieLightResponse>(response.RawBytes);
                        foreach (var movie in wrapper.Movies)
                        {
                            movie.TranslationLanguage = TmdbClient.DefaultLanguage;
                        }
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

                    var result = wrapper?.Movies ?? new List<MovieLightJson>();
                    ProcessTranslations(result);
                    var nbResult = wrapper?.TotalMovies ?? 0;
                    return (result, nbResult);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Process translations for a list of movies
        /// </summary>
        /// <param name="movies"></param>
        /// <returns></returns>
        private void ProcessTranslations(IEnumerable<IMovie> movies)
        {
            foreach (var movie in movies)
            {
                TranslateMovie(movie);
            }
        }

        /// <summary>
        /// Get similar movies
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="imdbIds">The imdbIds of the movies, split by comma</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Similar movies</returns>
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetSimilarAsync(int page,
            int limit,
            IEnumerable<string> imdbIds,
            CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new MovieLightResponse();
                    if (limit < 1 || limit > 50)
                        limit = Utils.Constants.MaxMoviesPerPage;

                    if (page < 1)
                        page = 1;

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}/{subsegment}", Method.POST);
                    request.AddUrlSegment("segment", "movies");
                    request.AddUrlSegment("subsegment", "similar");
                    request.AddQueryParameter("limit", limit.ToString());
                    request.AddQueryParameter("page", page.ToString());
                    request.AddJsonBody(imdbIds);

                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        wrapper = JsonSerializer.Deserialize<MovieLightResponse>(response.RawBytes);
                        foreach (var movie in wrapper.Movies)
                        {
                            movie.TranslationLanguage = TmdbClient.DefaultLanguage;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetSimilarAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetSimilarAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetSimilarAsync ({page}, {limit}, {string.Join(",", imdbIds)}) in {elapsedMs} milliseconds.");
                    }

                    var result = wrapper?.Movies ?? new List<MovieLightJson>();
                    ProcessTranslations(result);
                    var nbResult = wrapper?.TotalMovies ?? 0;
                    return (result, nbResult);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<MovieLightJson>(), 0);
            }
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
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> SearchMoviesAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new MovieLightResponse();
                    if (limit < 1 || limit > 50)
                        limit = Utils.Constants.MaxMoviesPerPage;

                    if (page < 1)
                        page = 1;

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}", Method.GET);
                    request.AddUrlSegment("segment", "movies");
                    request.AddParameter("limit", limit);
                    request.AddParameter("page", page);
                    if (genre != null) request.AddParameter("genre", genre.EnglishName);
                    request.AddParameter("minimum_rating", Convert.ToInt32(ratingFilter));
                    request.AddParameter("query_term", criteria);

                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        wrapper = JsonSerializer.Deserialize<MovieLightResponse>(response.RawBytes);
                        foreach (var movie in wrapper.Movies)
                        {
                            movie.TranslationLanguage = TmdbClient.DefaultLanguage;
                        }
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

                    var result = wrapper?.Movies ?? new List<MovieLightJson>();
                    ProcessTranslations(result);
                    var nbResult = wrapper?.TotalMovies ?? 0;
                    return (result, nbResult);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <returns>Task</returns>
        public void TranslateMovie(IMovie movieToTranslate)
        {
            if (TmdbClient.DefaultLanguage == "en" && movieToTranslate.TranslationLanguage == TmdbClient.DefaultLanguage) return;
            _moviesToTranslateObservable.OnNext(movieToTranslate);
        }

        /// <summary>
        /// Get the link to the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<string> GetMovieTrailerAsync(MovieJson movie, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var uri = string.Empty;
                    try
                    {
                        var tmdbMovie = await TmdbClient.GetMovieAsync(movie.ImdbCode, MovieMethods.Videos)
                            .ConfigureAwait(false);
                        var trailers = tmdbMovie?.Videos;
                        if (trailers != null && trailers.Results.Any())
                        {
                            using (var service = Client.For(YouTube.Default))
                            {
                                var videos =
                                    (await service.GetAllVideosAsync("https://youtube.com/watch?v=" + trailers.Results
                                                                         .FirstOrDefault()
                                                                         .Key).ConfigureAwait(false))
                                    .ToList();
                                if (videos.Any())
                                {
                                    var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                                    var maxRes = settings.DefaultHdQuality ? 1080 : 720;
                                    uri =
                                        await videos.Where(a => !a.Is3D && a.Resolution <= maxRes &&
                                                                a.Format == VideoFormat.Mp4 && a.AudioBitrate > 0)
                                            .Aggregate((i1, i2) => i1.Resolution > i2.Resolution ? i1 : i2)
                                            .GetUriAsync();
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
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get cast
        /// </summary>
        /// <param name="imdbCode">Tmdb cast Id</param>
        /// <returns><see cref="Person"/></returns>
        public async Task<Person> GetCast(string imdbCode)
        {
            try
            {
                var search = await TmdbClient.FindAsync(FindExternalSource.Imdb, $"nm{imdbCode}");
                return await TmdbClient.GetPersonAsync(search.PersonResults.FirstOrDefault().Id, PersonMethods.Images | PersonMethods.TaggedImages);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Get movies for a cast by its ImdbCode
        /// </summary>
        /// <param name="imdbCode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MovieLightJson>> GetMovieFromCast(string imdbCode, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Utils.Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new MovieLightResponse();
                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/movies/cast/{segment}", Method.GET);
                    request.AddUrlSegment("segment", imdbCode);
                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        wrapper = JsonSerializer.Deserialize<MovieLightResponse>(response.RawBytes);
                        foreach (var movie in wrapper.Movies)
                        {
                            movie.TranslationLanguage = TmdbClient.DefaultLanguage;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieFromCast cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieFromCast: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetMovieFromCast ({imdbCode}) in {elapsedMs} milliseconds.");
                    }

                    var result = wrapper?.Movies ?? new List<MovieLightJson>();
                    ProcessTranslations(result);
                    return result;
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new List<MovieLightJson>();
            }
        }

        /// <summary>
        /// Retrieve an image url from Tmdb
        /// </summary>
        /// <param name="url">Image to retrieve</param>
        /// <returns>Image url</returns>
        public string GetImagePathFromTmdb(string url)
        {
            return TmdbClient.GetImageUrl("original", url, true).AbsoluteUri;
        }
    }
}