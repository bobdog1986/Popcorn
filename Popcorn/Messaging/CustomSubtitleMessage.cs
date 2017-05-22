using GalaSoft.MvvmLight.Messaging;

namespace Popcorn.Messaging
{
    public class CustomSubtitleMessage : MessageBase
    {
        public string FileName { get; set; }

        public bool Error { get; set; }
    }
}
