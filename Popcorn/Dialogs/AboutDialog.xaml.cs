using System.Diagnostics;
using System.Windows.Input;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void CanGoToPage(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PerformGoToPage(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Parameter.ToString()));
            e.Handled = true;
        }
    }
}
