using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace Popcorn.Messaging
{
    public class ShowTraktDialogMessage : MessageBase
    {
        public bool? IsLoggedIn { get; set; }

        public ShowTraktDialogMessage(bool? isLoggedIn = null)
        {
            IsLoggedIn = isLoggedIn;
        }
    }
}
