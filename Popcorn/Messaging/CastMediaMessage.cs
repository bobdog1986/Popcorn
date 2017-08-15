using System;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Chromecast.Models;

namespace Popcorn.Messaging
{
    public class CastMediaMessage : MessageBase
    {
        public Action CloseCastDialog { get; set; }

        public CancellationTokenSource CastCancellationTokenSource { get; set; }

        public Func<ChromecastReceiver, Task> StartCast { get; set; }

        public ChromecastReceiver ChromecastReceiver { get; set; }
    }
}
