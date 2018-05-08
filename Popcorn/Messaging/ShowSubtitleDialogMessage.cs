using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Subtitles;
using System.Collections.Generic;

namespace Popcorn.Messaging
{
    public class ShowSubtitleDialogMessage : MessageBase
    {
        public readonly IEnumerable<Subtitle> Subtitles;

        public OSDB.Models.Subtitle SelectedSubtitle { get; set; }

        public OSDB.Models.Subtitle CurrentSubtitle { get; set; }

        public ShowSubtitleDialogMessage(IEnumerable<Subtitle> subtitles, OSDB.Models.Subtitle currentSubtitle)
        {
            Subtitles = subtitles;
            CurrentSubtitle = currentSubtitle;
        }
    }
}
