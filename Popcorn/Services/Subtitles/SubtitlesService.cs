using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CookComputing.XmlRpc;
using Polly;
using Popcorn.OSDB;

namespace Popcorn.Services.Subtitles
{
    /// <summary>
    /// The subtitles service
    /// </summary>
    public class SubtitlesService : ISubtitlesService
    {
        /// <summary>
        /// Get subtitles languages
        /// </summary>
        /// <returns>Languages</returns>
        public async Task<IEnumerable<Language>> GetSubLanguages()
        {
            var retryGetSubLanguagesPolicy = Policy
                .Handle<XmlRpcServerException>()
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            return await retryGetSubLanguagesPolicy.ExecuteAsync(async () =>
            {
                using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
                {
                    return await osdb.GetSubLanguages();
                }
            });
        }

        /// <summary>
        /// Search subtitles by imdb code and languages
        /// </summary>
        /// <param name="languages">Languages</param>
        /// <param name="imdbId">Imdb code</param>
        /// <param name="season">Season number</param>
        /// <param name="episode">Episode number</param>
        /// <returns>Subtitles</returns>
        public async Task<IList<Subtitle>> SearchSubtitlesFromImdb(string languages, string imdbId, int? season, int? episode)
        {
            var retrySearchSubtitlesFromImdbPolicy = Policy
                .Handle<XmlRpcServerException>()
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            return await retrySearchSubtitlesFromImdbPolicy.ExecuteAsync(async () =>
            {
                using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
                {
                    return await osdb.SearchSubtitlesFromImdb(languages, imdbId, season, episode);
                }
            });
        }

        /// <summary>
        /// Download a subtitle to a path
        /// </summary>
        /// <param name="path">Path to download</param>
        /// <param name="subtitle">Subtitle to download</param>
        /// <returns>Downloaded subtitle path</returns>
        public async Task<string> DownloadSubtitleToPath(string path, Subtitle subtitle)
        {
            var retryDownloadSubtitleToPathPolicy = Policy
                .Handle<XmlRpcServerException>()
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            return await retryDownloadSubtitleToPathPolicy.ExecuteAsync(async () =>
            {
                using (var osdb = new Osdb().Login("OSTestUserAgentTemp"))
                {
                    return await osdb.DownloadSubtitleToPath(path, subtitle);
                }
            });
        }
    }
}