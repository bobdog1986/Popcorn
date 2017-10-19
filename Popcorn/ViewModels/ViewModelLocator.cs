using System.Diagnostics.CodeAnalysis;
using Enterwell.Clients.Wpf.Notifications;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Popcorn.Services.Application;
using Popcorn.Services.Associations;
using Popcorn.Services.Cache;
using Popcorn.Services.Genres;
using Popcorn.Services.Hub;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.Movies.Trailer;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.Subtitles;
using Popcorn.Services.User;
using Popcorn.ViewModels.Pages.Home;
using Popcorn.ViewModels.Pages.Home.Movie;
using Popcorn.ViewModels.Pages.Home.Movie.Details;
using Popcorn.ViewModels.Pages.Home.Show;
using Popcorn.ViewModels.Pages.Home.Show.Details;
using Popcorn.ViewModels.Windows;
using Popcorn.ViewModels.Windows.Settings;
using Popcorn.Services.Shows.Trailer;
using Popcorn.Services.Trakt;
using Popcorn.ViewModels.Pages.Home.Cast;

namespace Popcorn.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            #region Services
            var movieService = new MovieService();
            var showService = new ShowService();
            SimpleIoc.Default.Register<IUserService>(() => new UserService(movieService, showService));
            SimpleIoc.Default.Register<IMovieService>(() => movieService);
            SimpleIoc.Default.Register<IShowService>(() => showService);
            SimpleIoc.Default.Register<IMovieTrailerService, MovieTrailerService>();
            SimpleIoc.Default.Register<IShowTrailerService, ShowTrailerService>();
            SimpleIoc.Default.Register<IApplicationService, ApplicationService>();
            SimpleIoc.Default.Register<ISubtitlesService, SubtitlesService>();
            SimpleIoc.Default.Register<IGenreService, GenreService>();
            SimpleIoc.Default.Register<ITraktService, TraktService>();
            SimpleIoc.Default.Register<IPopcornHubService, PopcornHubService>();
            SimpleIoc.Default.Register<ICacheService, CacheService>();
            SimpleIoc.Default.Register<IFileAssociationService, FileAssociationService>();
            SimpleIoc.Default.Register<NotificationMessageManager>();

            #endregion

            #region ViewModels

            SimpleIoc.Default.Register<WindowViewModel>();
            SimpleIoc.Default.Register<PagesViewModel>();

            SimpleIoc.Default.Register<MoviePageViewModel>();
            SimpleIoc.Default.Register<MovieDetailsViewModel>();

            SimpleIoc.Default.Register<ShowPageViewModel>();
            SimpleIoc.Default.Register<ShowDetailsViewModel>();

            SimpleIoc.Default.Register<CastViewModel>();

            SimpleIoc.Default.Register<ApplicationSettingsViewModel>();

            #endregion
        }

        /// <summary>
        /// Gets the Window property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public WindowViewModel Window => ServiceLocator.Current.GetInstance<WindowViewModel>();

        /// <summary>
        /// Gets the Pages property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PagesViewModel Pages => ServiceLocator.Current.GetInstance<PagesViewModel>();

        /// <summary>
        /// Gets the MovieDetails property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MovieDetailsViewModel MovieDetails => ServiceLocator.Current.GetInstance<MovieDetailsViewModel>();

        /// <summary>
        /// Gets the ShowDetails property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ShowDetailsViewModel ShowDetails => ServiceLocator.Current.GetInstance<ShowDetailsViewModel>();

        /// <summary>
        /// Gets the Cast property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public CastViewModel Cast => ServiceLocator.Current.GetInstance<CastViewModel>();

        /// <summary>
        /// Gets the ApplicationSettings property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ApplicationSettingsViewModel ApplicationSettings
            => ServiceLocator.Current.GetInstance<ApplicationSettingsViewModel>();

        /// <summary>
        /// Gets the NotificationMessageManager property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public NotificationMessageManager Manager
            => ServiceLocator.Current.GetInstance<NotificationMessageManager>();
    }
}