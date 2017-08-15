using System.IO;

namespace Popcorn.Utils
{
    /// <summary>
    /// Constants of the project
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// App version
        /// </summary>
        public const string AppVersion = "2.5.0";

        /// <summary>
        /// Endpoint to API
        /// </summary>
        public const string PopcornApi = "https://popcornapi.azurewebsites.net/api";

        /// <summary>
        /// Application Insights key
        /// </summary>
        public const string AiKey = "647b7610-bfc7-4b78-962d-822f7e59eda3";

        /// <summary>
        /// Client ID for TMDb
        /// </summary>
        public const string TmDbClientId = "a21fe922d3bac6654e93450e9a18af1c";

        /// <summary>
        /// In percentage, the minimum of buffering before we can actually start playing the movie
        /// </summary>
        public const double MinimumMovieBuffering = 10.0;

        /// <summary>
        /// In percentage, the minimum of buffering before we can actually start playing the episode
        /// </summary>
        public const double MinimumShowBuffering = 10.0;

        /// <summary>
        /// The maximum number of movies per page to load from the API
        /// </summary>
        public const int MaxMoviesPerPage = 20;

        /// <summary>
        /// The maximum number of shows per page to load from the API
        /// </summary>
        public const int MaxShowsPerPage = 20;

        /// <summary>
        /// Url of the server updates
        /// </summary>
        public const string GithubRepository = "https://github.com/bbougot/Popcorn";

        /// <summary>
        /// Directory of assets
        /// </summary>
        public static string Assets { get; } = Path.GetTempPath() + "Popcorn\\Assets\\";

        /// <summary>
        /// Directory of downloaded movies
        /// </summary>
        public static string MovieDownloads { get; } = Path.GetTempPath() + "Popcorn\\Downloads\\Movies\\";

        /// <summary>
        /// Directory of dropped files
        /// </summary>
        public static string DropFilesDownloads { get; } = Path.GetTempPath() + "Popcorn\\Downloads\\Dropped\\";

        /// <summary>
        /// Directory of downloaded shows
        /// </summary>
        public static string ShowDownloads { get; } = Path.GetTempPath() + "Popcorn\\Downloads\\Shows\\";

        /// <summary>
        /// Directory of downloaded movie torrents
        /// </summary>
        public static string MovieTorrentDownloads { get; } = Path.GetTempPath() + "Popcorn\\Torrents\\Movies\\";

        /// <summary>
        /// Subtitles directory
        /// </summary>
        public static string Subtitles { get; } = Path.GetTempPath() + "Popcorn\\Subtitles\\";
    }
}