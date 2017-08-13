using System;
using System.IO;
using System.Linq;
using NLog;

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
                    Directory.Delete(folder.FullName);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while deleting folder: {ex.Message}.");
            }
        }
    }
}