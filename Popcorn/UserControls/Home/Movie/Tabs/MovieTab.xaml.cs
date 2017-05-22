using System.Windows;
using System.Windows.Controls;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.UserControls.Home.Movie.Tabs
{
    /// <summary>
    /// Interaction logic for MovieTab.xaml
    /// </summary>
    public partial class MovieTab
    {
        /// <summary>
        /// Initializes a new instance of the MovieTab class.
        /// </summary>
        public MovieTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MovieTabsViewModel;
            if (vm == null) return;
            if (vm is PopularMovieTabViewModel || vm is GreatestMovieTabViewModel || vm is RecentMovieTabViewModel ||
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel)
            {
                if (!vm.IsLoadingMovies && vm.NeedSync)
                {
                    await vm.LoadMoviesAsync(true).ConfigureAwait(false);
                    vm.NeedSync = false;
                }
            }
            else if (vm is SearchMovieTabViewModel)
            {
                var searchVm = vm as SearchMovieTabViewModel;
                if (!searchVm.IsLoadingMovies && vm.NeedSync)
                {
                    await searchVm.LoadMoviesAsync(true).ConfigureAwait(false);
                    vm.NeedSync = false;
                }
            }
        }

        /// <summary>
        /// Load movies if control has reached bottom position
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private async void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var totalHeight = e.VerticalOffset + e.ViewportHeight;
            if (totalHeight < 2d / 3d * e.ExtentHeight) return;
            var vm = DataContext as MovieTabsViewModel;
            if (vm == null) return;
            if (vm is PopularMovieTabViewModel || vm is GreatestMovieTabViewModel || vm is RecentMovieTabViewModel ||
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel)
            {
                if (!vm.IsLoadingMovies)
                    await vm.LoadMoviesAsync().ConfigureAwait(false);
            }
            else if (vm is SearchMovieTabViewModel)
            {
                var searchVm = vm as SearchMovieTabViewModel;
                if (!searchVm.IsLoadingMovies)
                    await searchVm.LoadMoviesAsync().ConfigureAwait(false);
            }
        }
    }
}