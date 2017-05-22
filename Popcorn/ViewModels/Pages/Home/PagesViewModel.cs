using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using Popcorn.Helpers;
using Popcorn.ViewModels.Pages.Home.Movie;
using Popcorn.ViewModels.Pages.Home.Show;

namespace Popcorn.ViewModels.Pages.Home
{
    /// <summary>
    /// Represents a page
    /// </summary>
    public class PagesViewModel : ViewModelBase
    {
        /// <summary>
        /// The pages
        /// </summary>
        private ObservableCollection<IPageViewModel> _pages = new ObservableCollection<IPageViewModel>();

        /// <summary>
        /// Pages shown into the interface
        /// </summary>
        public ObservableCollection<IPageViewModel> Pages
        {
            get => _pages;
            private set { Set(() => Pages, ref _pages, value); }
        }
        
        /// <summary>
        /// Create an instance of <see cref="PagesViewModel"/>
        /// </summary>
        /// <param name="moviePage">Movie page</param>
        /// <param name="showPage">Show page</param>
        public PagesViewModel(MoviePageViewModel moviePage, ShowPageViewModel showPage)
        {
            moviePage.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("MoviesLabel");
            showPage.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("ShowsLabel");
            Pages = new ObservableCollection<IPageViewModel>
            {
                moviePage,
                showPage
            };
        }
    }
}