using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Popcorn.Helpers;
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
            if (!(DataContext is MovieTabsViewModel vm)) return;
            var split = "MovieTabViewModel";
            ApplicationInsightsHelper.TelemetryClient.TrackPageView(
                $"Movie Tab {vm.GetType().Name.Split(new[] {split}, StringSplitOptions.None).First()}");

            if (vm is PopularMovieTabViewModel || vm is GreatestMovieTabViewModel || vm is RecentMovieTabViewModel ||
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel ||
                vm is RecommendationsMovieTabViewModel)
            {
                if (!vm.IsLoadingMovies && vm.NeedSync)
                {
                    await vm.LoadMoviesAsync(true);
                    vm.NeedSync = false;
                }
            }
            else if (vm is SearchMovieTabViewModel)
            {
                var searchVm = vm as SearchMovieTabViewModel;
                if (!searchVm.IsLoadingMovies && vm.NeedSync)
                {
                    await searchVm.LoadMoviesAsync(true);
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
            if (!(DataContext is MovieTabsViewModel vm))
            {
                _semaphore.Release();
                return;
            }

            if (vm is PopularMovieTabViewModel || vm is GreatestMovieTabViewModel || vm is RecentMovieTabViewModel ||
                vm is FavoritesMovieTabViewModel || vm is SeenMovieTabViewModel ||
                vm is RecommendationsMovieTabViewModel)
            {
                if (!vm.IsLoadingMovies)
                    await vm.LoadMoviesAsync();
            }
            else if (vm is SearchMovieTabViewModel)
            {
                var searchVm = vm as SearchMovieTabViewModel;
                if (!searchVm.IsLoadingMovies)
                    await searchVm.LoadMoviesAsync();
            }

            _semaphore.Release();
        }
    }
}