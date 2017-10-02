using System;
using System.IO;
using System.Linq;
using NLog;
using Popcorn.Utils;

namespace Popcorn.Helpers
{
    public class FileHelper
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get directory size
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static long GetDirectorySize(string folderPath)
        {
            var di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        /// <summary>
        /// Create download folders
        /// </summary>
        public static void CreateFolders()
        {
            if (!Directory.Exists(Constants.Subtitles))
            {
                Directory.CreateDirectory(Constants.Subtitles);
            }

            if (!Directory.Exists(Constants.MovieDownloads))
            {
                Directory.CreateDirectory(Constants.MovieDownloads);
            }

            if (!Directory.Exists(Constants.DropFilesDownloads))
            {
                Directory.CreateDirectory(Constants.DropFilesDownloads);
            }

            if (!Directory.Exists(Constants.ShowDownloads))
            {
                Directory.CreateDirectory(Constants.ShowDownloads);
            }

            if (!Directory.Exists(Constants.MovieTorrentDownloads))
            {
                Directory.CreateDirectory(Constants.MovieTorrentDownloads);
            }
        }

        /// <summary>
        /// Clear download folders
        /// </summary>
        public static void ClearFolders()
        {
            if (Directory.Exists(Constants.Subtitles))
            {
                DeleteFolder(Constants.Subtitles);
            }

            if (Directory.Exists(Constants.MovieDownloads))
            {
                DeleteFolder(Constants.MovieDownloads);
            }

            if (Directory.Exists(Constants.DropFilesDownloads))
            {
                DeleteFolder(Constants.DropFilesDownloads);
            }

            if (Directory.Exists(Constants.ShowDownloads))
            {
                DeleteFolder(Constants.ShowDownloads);
            }

            if (Directory.Exists(Constants.MovieTorrentDownloads))
            {
                DeleteFolder(Constants.MovieTorrentDownloads);
            }
        }

        /// <summary>
        /// Delete folder and subfolders
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFolder(string path)
        {
            foreach (
                var filePath in Directory.GetFiles(path, "*.*",
                    SearchOption.AllDirectories)
            )
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error while deleting file: {ex.Message}.");
                }
            }

            try
            {
                var directory = new DirectoryInfo(path);
                var directories = directory.GetDirectories();

                foreach (var folder in directories)
                    Directory.Delete(folder.FullName, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while deleting folder: {ex.Message}.");
            }
        }
    }
}