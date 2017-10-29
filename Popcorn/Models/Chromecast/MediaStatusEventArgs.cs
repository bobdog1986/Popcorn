using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCast.Models.Media;

namespace Popcorn.Models.Chromecast
{
    public class MediaStatusEventArgs : EventArgs
    {
        public readonly MediaStatus Status;

        public MediaStatusEventArgs(MediaStatus status)
        {
            Status = status;
        }
    }
}
