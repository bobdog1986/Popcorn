using System.Diagnostics;
using System.Windows.Input;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour HelpDialog.xaml
    /// </summary>
    public partial class HelpDialog
    {
        public HelpDialog()
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
