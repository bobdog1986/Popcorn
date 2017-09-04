using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NuGet;
using Popcorn.Comparers;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Services.Application;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.User;
using Popcorn.ViewModels.Pages.Home.Movie.Tabs;

namespace Popcorn.ViewModels.Pages.Home.Show.Tabs
{
    public class RecommendationsShowTabViewModel : ShowTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the RecommendationsShowTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="showService">Show service</param>
        /// <param name="userService">Show history service</param>
        public RecommendationsShowTabViewModel(IApplicationService applicationService, IShowService showService,
            IUserService userService)
            : base(applicationService, showService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("RecommendationsTitleTab"))
        {
        }

        /// <summary>
        /// Load shows asynchronously
        /// </summary>
        public override async Task LoadShowsAsync(bool reset = false)
        {
            await LoadingSemaphore.WaitAsync();
            if (reset)
            {
                Shows.Clear();
                Page = 0;
                VerticalScroll = 0d;
            }

            var watch = Stopwatch.StartNew();
            Page++;
            if (Page > 1 && Shows.Count == MaxNumberOfShows)
            {
                Page--;
                LoadingSemaphore.Release();
                return;
            }

            StopLoadingShows();
            Logger.Info(
                $"Loading page {Page}...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingShows = true;
                await Task.Run(async () =>
                {
                    var getMoviesWatcher = new Stopwatch();
                    getMoviesWatcher.Start();
                    var result =
                        await ShowService.Discover(Page).ConfigureAwait(false);
                    getMoviesWatcher.Stop();
                    var getMoviesEllapsedTime = getMoviesWatcher.ElapsedMilliseconds;
                    if (reset && getMoviesEllapsedTime < 500)
                    {
                        // Wait for VerticalOffset to reach 0 (animation lasts 500ms)
                        await Task.Delay(500 - (int) getMoviesEllapsedTime).ConfigureAwait(false);
                    }

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        Shows.AddRange(result.Item1.Except(Shows, new ShowLightComparer()));
                        IsLoadingShows = false;
                        IsShowFound = Shows.Any();
                        CurrentNumberOfShows = Shows.Count;
                        MaxNumberOfShows = result.nbMovies;
                        UserService.SyncShowHistory(Shows);
                    });
                }).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded page {Page} in {elapsedMs} milliseconds.");
                LoadingSemaphore.Release();
            }
        }
    }
}