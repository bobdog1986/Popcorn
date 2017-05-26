using System;

namespace Popcorn.Events
{
    public class SubtitleChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The subtitle path
        /// </summary>
        public readonly string SubtitlePath;

        /// <summary>
        /// Initialize a new instance of SubtitleChangedEventArgs
        /// </summary>
        /// <param name="subtitlePath">Subtitle path</param>
        public SubtitleChangedEventArgs(string subtitlePath)
        {
            SubtitlePath = subtitlePath;
        }
    }
}
