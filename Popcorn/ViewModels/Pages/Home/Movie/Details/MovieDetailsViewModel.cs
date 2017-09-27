using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using NuGet;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.Movies.Trailer;
using Popcorn.Services.Subtitles;
using Popcorn.ViewModels.Pages.Home.Movie.Download;
using Popcorn.Models.Torrent.Movie;
using Popcorn.Services.Download;
using Popcorn.ViewModels.Windows.Settings;
using Subtitle = Popcorn.Models.Subtitles.Subtitle;

namespace Popcorn.ViewModels.Pages.Home.Movie.Details
{
    /// <summary>
    /// Manage the movie
    /// </summary>
    public class MovieDetailsViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The service used to interact with movies
        /// </summary>
        private readonly IMovieService _movieService;

        /// <summary>
        /// Manage the movie download
        /// </summary>
        private DownloadMovieViewModel _downloadMovie;

        /// <summary>
        /// Specify if a movie is downloading
        /// </summary>
        private bool _isDownloadingMovie;

        /// <summary>
        /// Specify if a movie is loading
        /// </summary>
        private bool _isMovieLoading;

        /// <summary>
        /// Specify if a trailer is playing
        /// </summary>
        private bool _isPlayingTrailer;

        /// <summary>
        /// Specify if a trailer is loading
        /// </summary>
        private bool _isTrailerLoading;

        /// <summary>
        /// True if similar movies are loading
        /// </summary>
        private bool _loadingSimilar;

        /// <summary>
        /// True if movie have similars
        /// </summary>
        private bool _anySimilar;

        /// <summary>
        /// The movie to manage
        /// </summary>
        private MovieJson _movie = new MovieJson();

        /// <summary>
        /// The similar movies
        /// </summary>
        private ObservableCollection<MovieLightJson> _similarMovies;

        /// <summary>
        /// The movie trailer service
        /// </summary>
        private readonly IMovieTrailerService _movieTrailerService;

        /// <summary>
        /// The service used to interact with subtitles
        /// </summary>
        private ISubtitlesService SubtitlesService { get; }

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        private bool _loadingSubtitles;

        /// <summary>
        /// Torrent health, from 0 to 10
        /// </summary>
        private double _torrentHealth;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The selected torrent
        /// </summary>
        private TorrentJson _selectedTorrent;

        /// <summary>
        /// Token to cancel movie loading
        /// </summary>
        private CancellationTokenSource CancellationLoadingToken { get; set; }

        /// <summary>
        /// Token to cancel trailer loading
        /// </summary>
        private CancellationTokenSource CancellationLoadingTrailerToken { get; set; }

        /// <summary>
        /// Initializes a new instance of the MovieDetailsViewModel class.
        /// </summary>
        /// <param name="movieService">Service used to interact with movies</param>
        /// <param name="movieTrailerService">The movie trailer service</param>
        /// <param name="subtitlesService">The subtitles service</param>
        public MovieDetailsViewModel(IMovieService movieService, IMovieTrailerService movieTrailerService,
            ISubtitlesService subtitlesService)
        {
            _movieTrailerService = movieTrailerService;
            _movieService = movieService;
            Movie = new MovieJson();
            SimilarMovies = new ObservableCollection<MovieLightJson>();
            SubtitlesService = subtitlesService;
            CancellationLoadingToken = new CancellationTokenSource();
            CancellationLoadingTrailerToken = new CancellationTokenSource();
            DownloadMovie = new DownloadMovieViewModel(subtitlesService, new DownloadMovieService<MovieJson>());
            RegisterMessages();
            RegisterCommands();
        }

        /// <summary>
        /// The selected movie to manage
        /// </summary>
        public MovieJson Movie
        {
            get => _movie;
            set { Set(() => Movie, ref _movie, value); }
        }

        /// <summary>
        /// The similar movies
        /// </summary>
        public ObservableCollection<MovieLightJson> SimilarMovies
        {
            get => _similarMovies;
            set { Set(() => SimilarMovies, ref _similarMovies, value); }
        }

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        public bool LoadingSubtitles
        {
            get => _loadingSubtitles;
            set { Set(() => LoadingSubtitles, ref _loadingSubtitles, value); }
        }

        /// <summary>
        /// Indicates if a movie is loading
        /// </summary>
        public bool IsMovieLoading
        {
            get => _isMovieLoading;
            set { Set(() => IsMovieLoading, ref _isMovieLoading, value); }
        }

        /// <summary>
        /// Torrent health, from 0 to 10
        /// </summary>
        public double TorrentHealth
        {
            get => _torrentHealth;
            set { Set(() => TorrentHealth, ref _torrentHealth, value); }
        }

        /// <summary>
        /// The selected torrent
        /// </summary>
        public TorrentJson SelectedTorrent
        {
            get => _selectedTorrent;
            set { Set(() => SelectedTorrent, ref _selectedTorrent, value); }
        }

        /// <summary>
        /// Manage the movie download
        /// </summary>
        public DownloadMovieViewModel DownloadMovie
        {
            get => _downloadMovie;
            set { Set(() => DownloadMovie, ref _downloadMovie, value); }
        }

        /// <summary>
        /// Specify if a trailer is loading
        /// </summary>
        public bool IsTrailerLoading
        {
            get => _isTrailerLoading;
            set { Set(() => IsTrailerLoading, ref _isTrailerLoading, value); }
        }

        /// <summary>
        /// Specify if a trailer is playing
        /// </summary>
        public bool IsPlayingTrailer
        {
            get => _isPlayingTrailer;
            set { Set(() => IsPlayingTrailer, ref _isPlayingTrailer, value); }
        }

        /// <summary>
        /// True if similar movies are loading
        /// </summary>
        public bool LoadingSimilar
        {
            get => _loadingSimilar;
            set { Set(() => LoadingSimilar, ref _loadingSimilar, value); }
        }

        /// <summary>
        /// True if movie have similars
        /// </summary>
        public bool AnySimilar
        {
            get => _anySimilar;
            set { Set(() => AnySimilar, ref _anySimilar, value); }
        }

        /// <summary>
        /// Specify if a movie is downloading
        /// </summary>
        public bool IsDownloadingMovie
        {
            get => _isDownloadingMovie;
            set { Set(() => IsDownloadingMovie, ref _isDownloadingMovie, value); }
        }

        /// <summary>
        /// Command used to load the movie
        /// </summary>
        public RelayCommand<IMovie> LoadMovieCommand { get; private set; }

        /// <summary>
        /// Command used to stop loading the trailer
        /// </summary>
        public RelayCommand StopLoadingTrailerCommand { get; private set; }

        /// <summary>
        /// Command used to play the movie
        /// </summary>
        public RelayCommand PlayMovieCommand { get; private set; }

        /// <summary>
        /// Command used to play the trailer
        /// </summary>
        public RelayCommand PlayTrailerCommand { get; private set; }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            StopLoadingMovie();
            StopPlayingMovie();
            StopLoadingTrailer();
            StopPlayingTrailer();
            base.Cleanup();
        }

        /// <summary>
        /// Load the movie's subtitles asynchronously
        /// </summary>
        /// <param name="movie">The movie</param>
        private async Task LoadSubtitles(MovieJson movie)
        {
            Logger.Debug(
                $"Load subtitles for movie: {movie.Title}");
            Movie = movie;
            LoadingSubtitles = true;
            try
            {
                var languages = (await SubtitlesService.GetSubLanguages().ConfigureAwait(false)).ToList();
                if (int.TryParse(new string(movie.ImdbCode
                    .SkipWhile(x => !char.IsDigit(x))
                    .TakeWhile(char.IsDigit)
                    .ToArray()), out int imdbId))
                {
                    var subtitles = await SubtitlesService.SearchSubtitlesFromImdb(
                        languages.Select(lang => lang.SubLanguageID).Aggregate((a, b) => a + "," + b),
                        imdbId.ToString(), null, null).ConfigureAwait(false);

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        movie.AvailableSubtitles =
                            new ObservableCollection<Subtitle>(subtitles.OrderBy(a => a.LanguageName)
                                .Select(sub => new Subtitle
                                {
                                    Sub = sub
                                })
                                .GroupBy(x => x.Sub.LanguageName,
                                    (k, g) =>
                                        g.Aggregate(
                                            (a, x) =>
                                                (Convert.ToDouble(x.Sub.Rating, CultureInfo.InvariantCulture) >=
                                                 Convert.ToDouble(a.Sub.Rating, CultureInfo.InvariantCulture))
                                                    ? x
                                                    : a)));

                        movie.AvailableSubtitles.Insert(0, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"),
                                SubtitleId = "none"
                            }
                        });

                        movie.AvailableSubtitles.Insert(1, new Subtitle
                        {
                            Sub = new OSDB.Subtitle
                            {
                                LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("CustomLabel"),
                                SubtitleId = "custom"
                            }
                        });

                        var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                        if (!string.IsNullOrEmpty(applicationSettings.DefaultSubtitleLanguage) &&
                            movie.AvailableSubtitles.Any(
                                a => a.Sub.LanguageName == applicationSettings.DefaultSubtitleLanguage))
                        {
                            movie.SelectedSubtitle =
                                movie.AvailableSubtitles.FirstOrDefault(
                                    a => a.Sub.LanguageName == applicationSettings.DefaultSubtitleLanguage);
                        }
                        else
                        {
                            movie.SelectedSubtitle = movie.AvailableSubtitles.FirstOrDefault();
                        }
                    });

                    LoadingSubtitles = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed loading subtitles for : {movie.Title}. {ex.Message}");
                LoadingSubtitles = false;
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    movie.AvailableSubtitles.Insert(0, new Subtitle
                    {
                        Sub = new OSDB.Subtitle
                        {
                            LanguageName = LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"),
                            SubtitleId = "none"
                        }
                    });

                    movie.SelectedSubtitle = movie.AvailableSubtitles.FirstOrDefault();
                });
            }
        }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<StopPlayingTrailerMessage>(
                this,
                message =>
                {
                    if (message.Type == Utils.MediaType.Movie)
                    {
                        StopPlayingTrailer();
                    }
                });

            Messenger.Default.Register<StopPlayingMovieMessage>(
                this,
                message => { StopPlayingMovie(); });

            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                message =>
                {
                    if (!string.IsNullOrEmpty(Movie?.ImdbCode))
                        _movieService.TranslateMovie(Movie);
                });

            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, e =>
            {
                if (e.PropertyName != GetPropertyName(() => Movie.WatchInFullHdQuality)) return;
                ComputeTorrentHealth();
            });
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            LoadMovieCommand = new RelayCommand<IMovie>(async movie => await LoadMovie(movie, CancellationLoadingToken.Token).ConfigureAwait(false));

            PlayMovieCommand = new RelayCommand(() =>
            {
                IsDownloadingMovie = true;
                Messenger.Default.Send(new DownloadMovieMessage(Movie));
            });

            PlayTrailerCommand = new RelayCommand(async () =>
            {
                await Task.Run(async () =>
                {
                    IsPlayingTrailer = true;
                    IsTrailerLoading = true;
                    await _movieTrailerService.LoadTrailerAsync(Movie, CancellationLoadingTrailerToken.Token).ConfigureAwait(false);
                    IsTrailerLoading = false;
                }).ConfigureAwait(false);
            });

            StopLoadingTrailerCommand = new RelayCommand(StopLoadingTrailer);
        }

        /// <summary>
        /// Load the requested movie
        /// </summary>
        /// <param name="movie">The movie to load</param>
        /// <param name="ct">Cancellation</param>
        private async Task LoadMovie(IMovie movie, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                Messenger.Default.Send(new LoadMovieMessage());
                Movie = new MovieJson();
                IsMovieLoading = true;
                SimilarMovies.Clear();
                await Task.Run(async () =>
                {
                    Movie = await _movieService.GetMovieAsync(movie.ImdbCode, ct).ConfigureAwait(false);
                    _movieService.TranslateMovie(Movie);
                    IsMovieLoading = false;
                    Movie.FullHdAvailable = Movie.Torrents.Any(torrent => torrent.Quality == "1080p");
                    var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                    Movie.WatchInFullHdQuality = Movie.FullHdAvailable && applicationSettings.DefaultHdQuality;
                    ComputeTorrentHealth();
                    var tasks = new Func<Task>[]
                    {
                        async () =>
                        {
                            LoadingSimilar = true;
                            var similars = await _movieService.GetMoviesSimilarAsync(Movie, ct).ConfigureAwait(false);
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                SimilarMovies.AddRange(similars);
                                AnySimilar = SimilarMovies.Any();
                                LoadingSimilar = false;
                            });
                        },
                        async () =>
                        {
                            await LoadSubtitles(Movie).ConfigureAwait(false);
                        }
                    };

                    await Task.WhenAll(tasks.Select(task => task()).ToArray()).ConfigureAwait(false);    
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed loading movie : {movie.ImdbCode}. {ex.Message}");
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug($"LoadMovie ({movie.ImdbCode}) in {elapsedMs} milliseconds.");
        }

        /// <summary>
        /// Compute torrent health
        /// </summary>
        private void ComputeTorrentHealth()
        {
            if (Movie.Torrents == null) return;

            SelectedTorrent = Movie.WatchInFullHdQuality
                ? Movie.Torrents.FirstOrDefault(a => a.Quality == "1080p")
                : Movie.Torrents.FirstOrDefault(a => a.Quality == "720p");
            if (SelectedTorrent != null && SelectedTorrent.Seeds < 4)
            {
                TorrentHealth = 0;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 6)
            {
                TorrentHealth = 1;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 8)
            {
                TorrentHealth = 2;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 10)
            {
                TorrentHealth = 3;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 12)
            {
                TorrentHealth = 4;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 14)
            {
                TorrentHealth = 5;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 16)
            {
                TorrentHealth = 6;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 18)
            {
                TorrentHealth = 7;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 20)
            {
                TorrentHealth = 8;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds < 22)
            {
                TorrentHealth = 9;
            }
            else if (SelectedTorrent != null && SelectedTorrent.Seeds >= 22)
            {
                TorrentHealth = 10;
            }
        }

        /// <summary>
        /// Stop loading the movie
        /// </summary>
        private void StopLoadingMovie()
        {
            Logger.Info(
                $"Stop loading movie: {Movie.Title}.");

            IsMovieLoading = false;
            CancellationLoadingToken.Cancel();
            CancellationLoadingToken.Dispose();
            CancellationLoadingToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Stop playing the movie's trailer
        /// </summary>
        private void StopLoadingTrailer()
        {
            Logger.Info(
                $"Stop loading movie's trailer: {Movie.Title}.");

            IsTrailerLoading = false;
            CancellationLoadingTrailerToken.Cancel();
            CancellationLoadingTrailerToken.Dispose();
            CancellationLoadingTrailerToken = new CancellationTokenSource();
            StopPlayingTrailer();
        }

        /// <summary>
        /// Stop playing the movie's trailer
        /// </summary>
        private void StopPlayingTrailer()
        {
            Logger.Info(
                $"Stop playing movie's trailer: {Movie.Title}.");

            IsPlayingTrailer = false;
        }

        /// <summary>
        /// Stop playing a movie
        /// </summary>
        private void StopPlayingMovie()
        {
            Logger.Info(
                $"Stop playing movie: {Movie.Title}.");

            IsDownloadingMovie = false;
            DownloadMovie.StopDownloadingMovie();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CancellationLoadingToken?.Dispose();
                CancellationLoadingTrailerToken?.Dispose();
            }

            _disposed = true;
        }
    }
}