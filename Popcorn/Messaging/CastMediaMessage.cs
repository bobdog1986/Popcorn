using GalaSoft.MvvmLight.Messaging;

namespace Popcorn.Messaging
{
    public class CastMediaMessage : MessageBase
    {
        public string Title { get; set; }

        public string MediaPath { get; set; }

        public string SubtitleFilePath { get; set; }

        public bool Cancelled { get; set; }

        public CastMediaMessage(string title, string mediaPath, string subtitleFilePath)
        {
            Title = title;
            SubtitleFilePath = subtitleFilePath;
            MediaPath = mediaPath;
        }
    }
}
