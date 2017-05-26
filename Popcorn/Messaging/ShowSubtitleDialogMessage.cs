using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Subtitles;
using System.Collections.Generic;

namespace Popcorn.Messaging
{
    public class ShowSubtitleDialogMessage : MessageBase
    {
        public readonly IEnumerable<Subtitle> Subtitles;

        public OSDB.Subtitle SelectedSubtitle { get; set; }

        public ShowSubtitleDialogMessage(IEnumerable<Subtitle> subtitles)
        {
            Subtitles = subtitles;
        }
    }
}
