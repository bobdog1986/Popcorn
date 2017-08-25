using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Subtitles;
using System.Collections.Generic;

namespace Popcorn.Messaging
{
    public class ShowSubtitleDialogMessage : MessageBase
    {
        public readonly IEnumerable<Subtitle> Subtitles;

        public OSDB.Subtitle SelectedSubtitle { get; set; }

        public OSDB.Subtitle CurrentSubtitle { get; set; }

        public ShowSubtitleDialogMessage(IEnumerable<Subtitle> subtitles, OSDB.Subtitle currentSubtitle)
        {
            Subtitles = subtitles;
            CurrentSubtitle = currentSubtitle;
        }
    }
}
