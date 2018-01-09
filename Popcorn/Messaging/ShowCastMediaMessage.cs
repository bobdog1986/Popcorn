using System;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GoogleCast;

namespace Popcorn.Messaging
{
    public class ShowCastMediaMessage : MessageBase
    {
        public Action CloseCastDialog { get; set; }

        public CancellationTokenSource CastCancellationTokenSource { get; set; }

        public Func<IReceiver, Task> StartCast { get; set; }

        public IReceiver ChromecastReceiver { get; set; }
    }
}
