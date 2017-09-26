using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CookComputing.XmlRpc;
using Popcorn.OSDB.Backend;
using Popcorn.Utils.Extensions;

namespace Popcorn.OSDB
{
    public class AnonymousClient : IAnonymousClient
    {
        private bool Disposed { get; set; }
        private IOsdb Proxy { get; }
        private string Token { get; set; }

        internal AnonymousClient(IOsdb proxy)
        {
            Proxy = proxy;
        }

        internal void Login(string username, string password, string language, string userAgent)
        {
            try
            {
                var response = Proxy.Login(username, password, language, userAgent);
                VerifyResponseCode(response);
                Token = response.token;
            }
            catch (Exception) { }
        }

        public Task<IList<Subtitle>> SearchSubtitlesFromImdb(string languages, string imdbId, int? season, int? episode)
        {
            if (string.IsNullOrEmpty(imdbId))
            {
                throw new ArgumentNullException(nameof(imdbId));
            }

            var request = new SearchSubtitlesRequest
            {
                sublanguageid = languages,
                imdbid = imdbId,
                episode = episode,
                season = season
            };

            return SearchSubtitlesInternal(request);
        }

        private Task<IList<Subtitle>> SearchSubtitlesInternal(SearchSubtitlesRequest request)
        {
            var tcs = new TaskCompletionSource<IList<Subtitle>>();
            try
            {
                var response = Proxy.SearchSubtitles(Token, new[] {request});
                VerifyResponseCode(response);

                var subtitles = new List<Subtitle>();
                var subtitlesInfo = response.data as object[];
                if (null != subtitlesInfo)
                {
                    foreach (var infoObject in subtitlesInfo)
                    {
                        var subInfo = SimpleObjectMapper.MapToObject<SearchSubtitlesInfo>((XmlRpcStruct) infoObject);
                        subtitles.Add(BuildSubtitleObject(subInfo));
                    }
                }
                tcs.TrySetResult(subtitles);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public Task<string> DownloadSubtitleToPath(string path, Subtitle subtitle)
        {
            return DownloadSubtitleToPath(path, subtitle, null);
        }

        private async Task<string> DownloadSubtitleToPath(string path, Subtitle subtitle, string newSubtitleName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (null == subtitle)
            {
                throw new ArgumentNullException(nameof(subtitle));
            }
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("path should point to a valid location");
            }

            var destinationfile = Path.Combine(path,
                (string.IsNullOrEmpty(newSubtitleName)) ? subtitle.SubtitleFileName : newSubtitleName);

            if (File.Exists(destinationfile))
            {
                //if file has been downloaded before - there is no need to download it again
                return destinationfile;
            }

            var tempZipName = Path.GetTempFileName();
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(subtitle.SubTitleDownloadLink))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var content = response.Content)
                        {
                            await content.ReadAsFileAsync(tempZipName, true);
                        }
                    }
                }

                UnZipSubtitleFileToFile(tempZipName, destinationfile);

            }
            finally
            {
                File.Delete(tempZipName);
            }

            return destinationfile;
        }

        public Task<IEnumerable<Language>> GetSubLanguages()
        {
            //get system language
            return GetSubLanguages("en");
        }

        private Task<IEnumerable<Language>> GetSubLanguages(string language)
        {
            var tcs = new TaskCompletionSource<IEnumerable<Language>>();
            try
            {
                var response = Proxy.GetSubLanguages(language);
                VerifyResponseCode(response);

                IList<Language> languages = response.data.Select(languageInfo => BuildLanguageObject(languageInfo))
                    .ToList();
                tcs.TrySetResult(languages);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing && !string.IsNullOrEmpty(Token))
                {
                    try
                    {
                        Proxy.Logout(Token);
                    }
                    catch
                    {
                        //soak it. We don't want exception on disposing. It's better to let the session timeout.
                    }
                    Token = null;
                }
                Disposed = true;
            }
        }

        ~AnonymousClient()
        {
            Dispose(false);
        }

        private void UnZipSubtitleFileToFile(string zipFileName, string subFileName)
        {
            using (FileStream subFile = File.OpenWrite(subFileName))
            using (FileStream tempFile = File.OpenRead(zipFileName))
            {
                var gzip = new GZipStream(tempFile, CompressionMode.Decompress);
                gzip.CopyTo(subFile);
            }
        }

        private Subtitle BuildSubtitleObject(SearchSubtitlesInfo info)
        {
            var sub = new Subtitle
            {
                SubtitleId = info.IDSubtitle,
                SubtitleHash = info.SubHash,
                SubtitleFileName = info.SubFileName,
                SubTitleDownloadLink = new Uri(info.SubDownloadLink),
                SubtitlePageLink = new Uri(info.SubtitlesLink),
                LanguageId = info.SubLanguageID,
                LanguageName = info.LanguageName,
                Rating = info.SubRating,
                Bad = info.SubBad,

                ImdbId = info.IDMovieImdb,
                MovieId = info.IDMovie,
                MovieName = info.MovieName,
                OriginalMovieName = info.MovieNameEng,
                MovieYear = int.Parse(info.MovieYear)
            };
            return sub;
        }

        private Language BuildLanguageObject(GetSubLanguagesInfo info)
        {
            var language = new Language
            {
                LanguageName = info.LanguageName,
                SubLanguageID = info.SubLanguageID,
                ISO639 = info.ISO639
            };
            return language;
        }

        private void VerifyResponseCode(ResponseBase response)
        {
            if (null == response)
            {
                throw new ArgumentNullException(nameof(response));
            }
            if (string.IsNullOrEmpty(response.status))
            {
                //aperantly there are some methods that dont define 'status'
                return;
            }

            int responseCode = int.Parse(response.status.Substring(0, 3));
            if (responseCode >= 400)
            {
                throw new OsdbException($"Unexpected error response {response.status}");
            }
        }
    }
}