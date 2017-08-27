using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Chromecast.Models
{
    public enum SourceType
    {
        Youtube,
        Torrent
    }

    public class ChromecastSession
    {
        public ChromecastReceiver Chromecast { get; set; }
        public string MediaPath { get; set; }
        public string MediaTitle { get; set; }
        public string SubtitlePath { get; set; }
        public SourceType SourceType { get; set; }
        public Func<object, Task<object>> OnCastSarted { get; set; }
        public Func<object, Task<object>> OnCastFailed { get; set; }
        public Func<object, Task<object>> OnStatusChanged { get; set; }
        public Func<object, Task<object>> CastServer { get; set; }
    }
}
