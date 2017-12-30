using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CookComputing.XmlRpc;
using NLog;
using Polly;
using Popcorn.OSDB;
using Popcorn.Utils;
using SubtitlesParser.Classes;

namespace Popcorn.Services.Subtitles
{
    /// <summary>
    /// The subtitles service
    /// </summary>
    public class SubtitlesService : ISubtitlesService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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

            try
            {
                return await retryGetSubLanguagesPolicy.ExecuteAsync(async () =>
                {
                    using (var osdb = await new Osdb().Login(Constants.OsdbUa))
                    {
                        return await osdb.GetSubLanguages();
                    }
                });
            }
            catch (Exception)
            {
                return new List<Language>();
            }
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
                using (var osdb = await new Osdb().Login(Constants.OsdbUa))
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
        /// <param name="remote">Is remote download path</param>
        /// <returns>Downloaded subtitle path</returns>
        public async Task<string> DownloadSubtitleToPath(string path, Subtitle subtitle, bool remote = true)
        {
            var retryDownloadSubtitleToPathPolicy = Policy
                .Handle<XmlRpcServerException>()
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            return await retryDownloadSubtitleToPathPolicy.ExecuteAsync(async () =>
            {
                using (var osdb = await new Osdb().Login(Constants.OsdbUa))
                {
                    return await osdb.DownloadSubtitleToPath(path, subtitle, remote);
                }
            });
        }

        /// <summary>
        /// Get captions from local path
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public IEnumerable<SubtitleItem> LoadCaptions(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return new List<SubtitleItem>();
            var parser = new SubtitlesParser.Classes.Parsers.SubParser();
            using (var fileStream = File.OpenRead(filePath))
            {
                return parser.ParseStream(fileStream, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Convert a .srt file to a .vtt file
        /// </summary>
        /// <param name="sFilePath">Path to the file</param>
        /// <returns>Path to the converted vtt file</returns>
        public string ConvertSrtToVtt(string sFilePath)
        {
            try
            {
                var path = Path.ChangeExtension(sFilePath, "vtt");
                using (var strReader = new StreamReader(sFilePath, Encoding.UTF8))
                using (var strWriter = new StreamWriter(File.Create(path), Encoding.UTF8))
                {
                    var rgxDialogNumber = new Regex(@"^\d+$");
                    var rgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d,\d\d\d) --> (\d\d:\d\d:\d\d,\d\d\d)");

                    // Write starting line for the WebVTT file
                    strWriter.WriteLine("WEBVTT");
                    strWriter.WriteLine("");

                    // Handle each line of the SRT file
                    string sLine;
                    while ((sLine = strReader.ReadLine()) != null)
                    {
                        // We only care about lines that aren't just an integer (aka ignore dialog id number lines)
                        if (rgxDialogNumber.IsMatch(sLine))
                            continue;

                        // If the line is a time frame line, reformat and output the time frame
                        Match match = rgxTimeFrame.Match(sLine);
                        if (match.Success)
                        {
                            sLine = sLine.Replace(',', '.'); // Simply replace the comma in the time with a period
                        }

                        strWriter.WriteLine(sLine); // Write out the line
                    }

                    return path;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }
    }
}