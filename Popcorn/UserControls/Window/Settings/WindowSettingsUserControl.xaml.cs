using System;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.UserControls.Window.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class WindowSettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the Settings class.
        /// </summary>
        public WindowSettingsUserControl()
        {
            InitializeComponent();
        }

        private void OnColorChanged(object sender, EventArgs e)
        {
            var vm = DataContext as ApplicationSettingsViewModel;
            if (vm == null) return;
            vm.SubtitlesColor = ColorPicker.Color;
        }
    }
}