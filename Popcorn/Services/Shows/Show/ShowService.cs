using System;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Genres;
using Popcorn.Models.Shows;
using RestSharp;
using TMDbLib.Client;
using Popcorn.Models.User;
using System.Linq;
using GalaSoft.MvvmLight.Ioc;
using Polly;
using Polly.Timeout;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Windows.Settings;
using Popcorn.YTVideoProvider;
using TMDbLib.Objects.TvShows;
using Utf8Json;
using Video = TMDbLib.Objects.General.Video;

namespace Popcorn.Services.Shows.Show
{
    public class ShowService : IShowService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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
        /// Constructor
        /// </summary>
        public ShowService()
        {
            TmdbClient = new TMDbClient(Utils.Constants.TmDbClientId, true)
            {
                MaxRetryCount = 50
            };

            Task.Run(() =>
            {
                try
                {
                    TmdbClient.GetConfig();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        /// <summary>
        /// Get show by its Imdb code
        /// </summary>
        /// <param name="imdbId">Show's Imdb code</param>
        /// <returns>The show</returns>
        public async Task<ShowJson> GetShowAsync(string imdbId, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}/{show}", Method.GET);
                    request.AddUrlSegment("segment", "shows");
                    request.AddUrlSegment("show", imdbId);
                    var show = new ShowJson();
                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        show = JsonSerializer.Deserialize<ShowJson>(response.RawBytes);
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetShowAsync ({imdbId}) in {elapsedMs} milliseconds.");
                    }

                    return show;
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get show light by its Imdb code
        /// </summary>
        /// <param name="imdbId">Show's Imdb code</param>
        /// <returns>The show</returns>
        public async Task<ShowLightJson> GetShowLightAsync(string imdbId, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}/light/{show}", Method.GET);
                    request.AddUrlSegment("segment", "shows");
                    request.AddUrlSegment("show", imdbId);
                    var show = new ShowLightJson();
                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        show = JsonSerializer.Deserialize<ShowLightJson>(response.RawBytes);
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowLightAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowLightAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetShowLightAsync ({imdbId}) in {elapsedMs} milliseconds.");
                    }

                    return show;
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get popular shows by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of shows to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular shows and the number of shows found</returns>
        public async Task<(IEnumerable<ShowLightJson> shows, int nbShows)> GetShowsAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new ShowLightResponse();
                    if (limit < 1 || limit > 50)
                        limit = Utils.Constants.MaxShowsPerPage;

                    if (page < 1)
                        page = 1;

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}", Method.GET);
                    request.AddUrlSegment("segment", "shows");
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

                        wrapper = JsonSerializer.Deserialize<ShowLightResponse>(response.RawBytes);
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowsAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowsAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetShowsAsync ({page}, {limit}) in {elapsedMs} milliseconds.");
                    }

                    var shows = wrapper?.Shows ?? new List<ShowLightJson>();
                    var nbShows = wrapper?.TotalShows ?? 0;
                    return (shows, nbShows);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Search shows by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched shows and the number of movies found</returns>
        public async Task<(IEnumerable<ShowLightJson> shows, int nbShows)> SearchShowsAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var wrapper = new ShowLightResponse();
                    if (limit < 1 || limit > 50)
                        limit = Utils.Constants.MaxShowsPerPage;

                    if (page < 1)
                        page = 1;

                    var restClient = new RestClient(Utils.Constants.PopcornApi);
                    var request = new RestRequest("/{segment}", Method.GET);
                    request.AddUrlSegment("segment", "shows");
                    request.AddParameter("limit", limit);
                    request.AddParameter("page", page);
                    if (genre != null) request.AddParameter("genre", genre.EnglishName);
                    request.AddParameter("minimum_rating", Convert.ToInt32(ratingFilter));
                    request.AddParameter("query_term", criteria);
                    try
                    {
                        var response = await restClient.ExecuteTaskAsync(request, cancellation)
                            .ConfigureAwait(false);
                        if (response.ErrorException != null)
                            throw response.ErrorException;

                        wrapper = JsonSerializer.Deserialize<ShowLightResponse>(response.RawBytes);
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "SearchShowsAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"SearchShowsAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"SearchShowsAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
                    }

                    var result = wrapper?.Shows ?? new List<ShowLightJson>();
                    var nbResult = wrapper?.TotalShows ?? 0;
                    return (result, nbResult);
                }, ct).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get the link to the youtube trailer of a show
        /// </summary>
        /// <param name="show">The show</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<string> GetShowTrailerAsync(ShowJson show, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var uri = string.Empty;
                    try
                    {
                        var shows = await TmdbClient.SearchTvShowAsync(show.Title).ConfigureAwait(false);
                        if (shows.Results.Any())
                        {
                            Video trailer = null;
                            foreach (var tvShow in shows.Results)
                            {
                                try
                                {
                                    var result = await TmdbClient.GetTvShowExternalIdsAsync(tvShow.Id)
                                        .ConfigureAwait(false);
                                    if (result.ImdbId == show.ImdbId)
                                    {
                                        var videos = await TmdbClient.GetTvShowVideosAsync(result.Id)
                                            .ConfigureAwait(false);
                                        if (videos != null && videos.Results.Any())
                                        {
                                            trailer = videos.Results.FirstOrDefault();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                }
                            }

                            if (trailer != null)
                            {
                                using (var service = Client.For(YouTube.Default))
                                {
                                    var videos = (await service
                                            .GetAllVideosAsync("https://youtube.com/watch?v=" + trailer.Key)
                                            .ConfigureAwait(false))
                                        .ToList();
                                    if (videos.Any())
                                    {
                                        var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                                        var maxRes = settings.DefaultHdQuality ? 1080 : 720;
                                        uri =
                                            await videos
                                                .Where(a => !a.Is3D && a.Resolution <= maxRes &&
                                                            a.Format == VideoFormat.Mp4 &&
                                                            a.AudioBitrate > 0)
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
                    }
                    catch (Exception exception) when (exception is TaskCanceledException ||
                                                      exception is OperationCanceledException)
                    {
                        Logger.Debug(
                            "GetShowTrailerAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowTrailerAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Debug(
                            $"GetShowTrailerAsync ({show.ImdbId}) in {elapsedMs} milliseconds.");
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
    }
}