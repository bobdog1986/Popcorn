using System;
using System.Collections.Generic;
using RestSharp.Deserializers;

namespace Popcorn.Models.User
{
    public class UserJson
    {
        [DeserializeAs(Name = "MachineGuid")]
        public Guid MachineGuid { get; set; }

        [DeserializeAs(Name = "DownloadLimit")]
        public int DownloadLimit { get; set; }

        [DeserializeAs(Name = "UploadLimit")]
        public int UploadLimit { get; set; }

        [DeserializeAs(Name = "DefaultHdQuality")]
        public bool DefaultHdQuality { get; set; }

        [DeserializeAs(Name = "DefaultSubtitleLanguage")]
        public string DefaultSubtitleLanguage { get; set; }

        [DeserializeAs(Name = "Language")]
        public LanguageJson Language { get; set; }

        [DeserializeAs(Name = "MovieHistory")]
        public List<MovieHistoryJson> MovieHistory { get; set; }

        [DeserializeAs(Name = "ShowHistory")]
        public List<ShowHistoryJson> ShowHistory { get; set; }
    }
}
