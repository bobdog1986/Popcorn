using System;
using System.Collections.Generic;
using Popcorn.Models.Subtitles;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class User
    {
        public int DownloadLimit { get; set; }

        public int UploadLimit { get; set; }

        public bool DefaultHdQuality { get; set; }

        public string DefaultSubtitleLanguage { get; set; }

        public string DefaultSubtitleColor { get; set; }

        public SubtitleSize DefaultSubtitleSize { get; set; }

        public Language Language { get; set; }

        public List<MovieHistory> MovieHistory { get; set; }

        public List<ShowHistory> ShowHistory { get; set; }

        public string CacheLocation { get; set; }

        public bool EnableTorrentFileAssociation { get; set; }

        public bool EnableMagnetLinkAssociation { get; set; }
    }
}
