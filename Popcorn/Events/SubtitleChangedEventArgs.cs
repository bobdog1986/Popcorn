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
        /// The subtitle
        /// </summary>
        public readonly OSDB.Subtitle Subtitle;

        /// <summary>
        /// Initialize a new instance of SubtitleChangedEventArgs
        /// </summary>
        /// <param name="subtitlePath">Subtitle path</param>
        /// <param name="subtitle">The subtitle</param>
        public SubtitleChangedEventArgs(string subtitlePath, OSDB.Subtitle subtitle)
        {
            SubtitlePath = subtitlePath;
            Subtitle = subtitle;
        }
    }
}
