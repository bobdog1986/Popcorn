using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.UserControls.Home.Movie.Tabs
{
    /// <summary>
    /// Interaction logic for MovieTab.xaml
    /// </summary>
    public partial class MovieTab
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

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
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel || vm is RecommendationsMovieTabViewModel)
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
            if (e.VerticalChange < 0 || totalHeight < 2d / 3d * e.ExtentHeight)
            {
                return;
            }

            if (_semaphore.CurrentCount == 0)
            {
                return;
            }

            await _semaphore.WaitAsync();
            var vm = DataContext as MovieTabsViewModel;
            if (vm == null)
            {
                _semaphore.Release();
                return;
            }

            if (vm is PopularMovieTabViewModel || vm is GreatestMovieTabViewModel || vm is RecentMovieTabViewModel ||
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel || vm is RecommendationsMovieTabViewModel)
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

            _semaphore.Release();
        }
    }
}