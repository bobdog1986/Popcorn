using System;
using GalaSoft.MvvmLight.Ioc;
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
        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var dc = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
            if (dc != null)
            {
                await dc.InitializeAsync();
                InitializeComponent();
            }
        }
    }
}