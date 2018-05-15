using System;
using GalaSoft.MvvmLight.Ioc;
using Popcorn.ViewModels.Pages.Home.Settings;

namespace Popcorn.UserControls.Home.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the Settings class.
        /// </summary>
        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var dc = SimpleIoc.Default.GetInstance<SettingsPageViewModel>();
            if (dc != null)
            {
                await dc.InitializeAsync();
                InitializeComponent();
            }
        }
    }
}