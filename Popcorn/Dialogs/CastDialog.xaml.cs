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
        }

        private void MediaPlayerPlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            e.CanExecute = vm.CanPlay;
        }

        private async void MediaPlayerPlayExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            await vm.PlayPause();
        }

        private void MediaPlayerPauseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            e.CanExecute = vm.CanPause;
        }

        private async void MediaPlayerPauseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            await vm.Pause();
        }

        private async void MediaPlayerSliderProgress_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            if (vm.CanPlay)
            {
                await vm.PlayPause();
            }
        }

        private async void MediaPlayerSliderProgress_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            if (vm.CanPause)
            {
                await vm.Pause();
            }
        }

        private async void MediaPlayerSliderProgress_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var vm = DataContext as ChromecastDialogViewModel;
            if (vm == null) return;

            await vm.Seek(e.NewValue);
        }
    }
}