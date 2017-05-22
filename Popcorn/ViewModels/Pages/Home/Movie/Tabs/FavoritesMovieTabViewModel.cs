using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NuGet;
using Popcorn.Comparers;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using Popcorn.Services.Application;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Movie.Tabs
{
    public class FavoritesMovieTabViewModel : MovieTabsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the FavoritesMovieTabViewModel class.
        /// </summary>
        /// <param name="applicationService">Application state</param>
        /// <param name="movieService">Movie service</param>
        /// <param name="userService">Movie history service</param>
        public FavoritesMovieTabViewModel(IApplicationService applicationService, IMovieService movieService,
            IUserService userService)
            : base(applicationService, movieService, userService,
                () => LocalizationProviderHelper.GetLocalizedValue<string>("FavoritesTitleTab"))
        {
            Messenger.Default.Register<ChangeFavoriteMovieMessage>(
                this,
                message =>
                {
                    Task.Run(async () =>
                    {
                        var movies = await UserService.GetFavoritesMovies(Page);
                        DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                        {
                            MaxNumberOfMovies = movies.nbMovies;
                            NeedSync = true;
                            await LoadMoviesAsync();
                        });
                    });
                });
        }

        /// <summary>
        /// Load movies asynchronously
        /// </summary>
        public override async Task LoadMoviesAsync(bool reset = false)
        {
            await LoadingSemaphore.WaitAsync();
            StopLoadingMovies();
            if (reset)
            {
                Movies.Clear();
                Page = 0;
            }

            var watch = Stopwatch.StartNew();
            Page++;
            if (Page > 1 && Movies.Count == MaxNumberOfMovies)
            {
                Page--;
                LoadingSemaphore.Release();
                return;
            }

            Logger.Info(
                "Loading movies favorites page...");
            HasLoadingFailed = false;
            try
            {
                IsLoadingMovies = true;
                var imdbIds =
                    await UserService.GetFavoritesMovies(Page);
                if (!NeedSync)
                {
                    var movies = new List<MovieJson>();
                    await imdbIds.movies.ParallelForEachAsync(async imdbId =>
                    {
                        var movie = await MovieService.GetMovieAsync(imdbId);
                        if (movie != null)
                        {
                            movie.IsFavorite = true;
                            movies.Add(movie);
                        }
                    });
                    var updatedMovies = movies.OrderBy(a => a.Title)
                        .Where(a => (Genre != null
                                        ? a.Genres.Any(
                                            genre => genre.ToLowerInvariant() ==
                                                     Genre.EnglishName.ToLowerInvariant())
                                        : a.Genres.TrueForAll(b => true)) && a.Rating >= Rating);
                    Movies.AddRange(updatedMovies.Except(Movies.ToList(), new MovieComparer()));
                }
                else
                {
                    var moviesToDelete = Movies.Select(a => a.ImdbCode).Except(imdbIds.allMovies);
                    var moviesToAdd = imdbIds.allMovies.Except(Movies.Select(a => a.ImdbCode));
                    foreach (var movie in moviesToDelete.ToList())
                    {
                        Movies.Remove(Movies.FirstOrDefault(a => a.ImdbCode == movie));
                    }

                    var movies = moviesToAdd.ToList();
                    await movies.ParallelForEachAsync(async imdbId =>
                        {
                            var movie = await MovieService.GetMovieAsync(imdbId);
                            if ((Genre != null
                                    ? movie.Genres.Any(
                                        genre => genre.ToLowerInvariant() ==
                                                 Genre.EnglishName.ToLowerInvariant())
                                    : movie.Genres.TrueForAll(b => true)) && movie.Rating >= Rating)
                            {
                                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                {
                                    Movies.Add(movie);
                                });
                            }
                        });
                }

                IsLoadingMovies = false;
                IsMovieFound = Movies.Any();
                CurrentNumberOfMovies = Movies.Count;
                MaxNumberOfMovies = imdbIds.nbMovies;
                await UserService.SyncMovieHistoryAsync(Movies);
            }
            catch (Exception exception)
            {
                Page--;
                Logger.Error(
                    $"Error while loading movies favorite page {Page}: {exception.Message}");
                HasLoadingFailed = true;
                Messenger.Default.Send(new ManageExceptionMessage(exception));
            }
            finally
            {
                Movies.Sort((a, b) => String.Compare(a.Title, b.Title, StringComparison.Ordinal));
                NeedSync = false;
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Info(
                    $"Loaded movies favorite page {Page} in {elapsedMs} milliseconds.");
                LoadingSemaphore.Release();
            }
        }
    }
}