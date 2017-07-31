using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Utils.Exceptions;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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
            try
            {
                Process.Start(new ProcessStartInfo(e.Parameter.ToString()));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(ex.Message)));
            }
        }
    }
}
