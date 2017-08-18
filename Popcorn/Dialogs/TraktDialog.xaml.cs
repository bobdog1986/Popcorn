using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Extensions;
using Popcorn.ViewModels.Dialogs;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour TraktDialog.xaml
    /// </summary>
    public partial class TraktDialog
    {
        public TraktDialog()
        {
            InitializeComponent();
            Browser.FrameLoadEnd += OnFrameLoaded;
        }

        private async void OnFrameLoaded(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            var source = await Browser.GetBrowser().MainFrame.GetSourceAsync();
            if (source.Contains("pin-code"))
            {
                var code = source.GetStringBetween("<div class=\"bottom-wrapper pin-code\">", "</div>");
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var vm = DataContext as TraktDialogViewModel;
                    if (vm != null)
                    {
                        await vm.ValidateOAuthCode(code);
                    }
                });
            }
        }
    }
}