using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.StaticFiles.ContentTypes;

namespace Popcorn.Services.Server
{
    public class CustomContentTypeProvider : FileExtensionContentTypeProvider
    {
        public CustomContentTypeProvider()
        {
            Mappings.Add(".vtt", "text/vtt");
            Mappings.Add(".srt", "text/plain");
            Mappings.Add(".torrent", "application/x-bittorrent");
        }
    }
}
