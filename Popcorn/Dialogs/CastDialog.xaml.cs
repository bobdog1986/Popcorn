using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Popcorn.ViewModels.Dialogs;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour CastDialog.xaml
    /// </summary>
    public partial class CastDialog
    {
        public CastDialog()
        {
            InitializeComponent();
            Loaded += CastDialog_Loaded;
        }

        private async void CastDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm != null)
            {
                await vm.LoadChromecasts();
            }
        }
    }
}