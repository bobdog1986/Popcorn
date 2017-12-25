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
using Popcorn.ViewModels.Dialogs;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour SubtitleDialog.xaml
    /// </summary>
    public partial class SubtitleDialog
    {
        public SubtitleDialog()
        {
            InitializeComponent();
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as SubtitleDialogViewModel;
            vm?.OnCloseAction.Invoke();
        }
    }
}
