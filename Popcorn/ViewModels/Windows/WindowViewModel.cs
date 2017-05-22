using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using NLog;
using Popcorn.Dialogs;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Services.Application;
using Popcorn.Services.FileServer;
using Popcorn.Services.User;
using Popcorn.Utils;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Dialogs;
using Popcorn.ViewModels.Pages.Home;
using Popcorn.ViewModels.Pages.Home.Movie;
using Popcorn.ViewModels.Pages.Home.Show;
using Popcorn.ViewModels.Pages.Player;
using Squirrel;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.ViewModels.Windows
{
    /// <summary>
    /// Window applcation's viewmodel
    /// </summary>
    public class WindowViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Holds the async message relative to <see cref="CustomSubtitleMessage"/>
        /// </summary>
        private IDisposable _customSubtitleMessage;

        /// <summary>
        /// Holds the async message relative to <see cref="CastMediaMessage"/>
        /// </summary>
        private IDisposable _castMediaMessage;

        /// <summary>
        /// Used to define the dialog context
        /// </summary>
        private readonly IDialogCoordinator _dialogCoordinator;

        /// <summary>
        /// Specify if an exception is curently managed
        /// </summary>
        private bool _isManagingException;

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        private bool _isMovieFlyoutOpen;

        /// <summary>
        /// Specify if show flyout is open
        /// </summary>
        private bool _isShowFlyoutOpen;

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        private bool _isSettingsFlyoutOpen;

        /// <summary>
        /// Application state
        /// </summary>
        private IApplicationService _applicationService;

        /// <summary>
        /// File server service
        /// </summary>
        private readonly IFileServerService _fileServerService;

        /// <summary>
        /// The movie history service
        /// </summary>
        private readonly IUserService _userService;

        /// <summary>
        /// <see cref="MediaPlayer"/>
        /// </summary>
        private MediaPlayerViewModel _mediaPlayer;

        /// <summary>
        /// Ignore taskbar on maximize 
        /// </summary>
        private bool _ignoreTaskbarOnMaximize;

        /// <summary>
        /// Main frame navigation service
        /// </summary>
        public NavigationService NavigationService { get; set; }

        /// <summary>
        /// Initializes a new instance of the WindowViewModel class.
        /// </summary>
        /// <param name="applicationService">Instance of Application state</param>
        /// <param name="userService">Instance of movie history service</param>
        /// <param name="fileServerService">Instance of file server service</param>
        public WindowViewModel(IApplicationService applicationService, IUserService userService, IFileServerService fileServerService)
        {
            _fileServerService = fileServerService;
            _userService = userService;
            _dialogCoordinator = DialogCoordinator.Instance;
            _applicationService = applicationService;
            RegisterMessages();
            RegisterCommands();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            ClearFolders();
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService
        {
            get => _applicationService;
            set { Set(() => ApplicationService, ref _applicationService, value); }
        }

        /// <summary>
        /// Ignore taskbar on maximize 
        /// </summary>
        public bool IgnoreTaskbarOnMaximize
        {
            get => _ignoreTaskbarOnMaximize;
            set { Set(() => IgnoreTaskbarOnMaximize, ref _ignoreTaskbarOnMaximize, value); }
        }

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        public bool IsSettingsFlyoutOpen
        {
            get => _isSettingsFlyoutOpen;
            set { Set(() => IsSettingsFlyoutOpen, ref _isSettingsFlyoutOpen, value); }
        }

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        public bool IsMovieFlyoutOpen
        {
            get => _isMovieFlyoutOpen;
            set { Set(() => IsMovieFlyoutOpen, ref _isMovieFlyoutOpen, value); }
        }

        /// <summary>
        /// Specify if show flyout is open
        /// </summary>
        public bool IsShowFlyoutOpen
        {
            get => _isShowFlyoutOpen;
            set { Set(() => IsShowFlyoutOpen, ref _isShowFlyoutOpen, value); }
        }

        /// <summary>
        /// Media player
        /// </summary>
        public MediaPlayerViewModel MediaPlayer
        {
            get => _mediaPlayer;
            set { Set(() => MediaPlayer, ref _mediaPlayer, value); }
        }

        /// <summary>
        /// Command used to close movie page
        /// </summary>
        public RelayCommand CloseMoviePageCommand { get; private set; }

        /// <summary>
        /// Command used to close show page
        /// </summary>
        public RelayCommand CloseShowPageCommand { get; private set; }

        /// <summary>
        /// Command used to close the application
        /// </summary>
        public RelayCommand MainWindowClosingCommand { get; private set; }

        /// <summary>
        /// Command used to open application settings
        /// </summary>
        public RelayCommand OpenSettingsCommand { get; private set; }

        /// <summary>
        /// Command used to open about dialog
        /// </summary>
        public RelayCommand OpenAboutCommand { get; private set; }

        /// <summary>
        /// Command used to open help dialog
        /// </summary>
        public RelayCommand OpenHelpCommand { get; private set; }

        /// <summary>
        /// Command used to load tabs
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Command used to drop files
        /// </summary>
        public RelayCommand<DragEventArgs> DropFileCommand { get; private set; }

        /// <summary>
        /// Command used to manage drag enter
        /// </summary>
        public RelayCommand<DragEventArgs> DragEnterFileCommand { get; private set; }

        /// <summary>
        /// Command used to manage drag leave
        /// </summary>
        public RelayCommand<DragEventArgs> DragLeaveFileCommand { get; private set; }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ManageExceptionMessage>(this, e => ManageException(e.UnHandledException));

            Messenger.Default.Register<LoadMovieMessage>(this, e => IsMovieFlyoutOpen = true);

            Messenger.Default.Register<LoadShowMessage>(this, e => IsShowFlyoutOpen = true);

            Messenger.Default.Register<PlayShowEpisodeMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(
                async () =>
                {
                    MediaPlayer = new MediaPlayerViewModel(_applicationService, message.Episode.FilePath,
                        message.Episode.Title,
                        MediaType.Show,
                        () =>
                        {
                            Messenger.Default.Send(new StopPlayingEpisodeMessage());
                        },
                        () =>
                        {
                            Messenger.Default.Send(new StopPlayingEpisodeMessage());
                        },
                        message.BufferProgress,
                        message.BandwidthRate,
                        message.Episode.SelectedSubtitle?.FilePath);

                    ApplicationService.IsMediaPlaying = true;
                    IsShowFlyoutOpen = false;
                    if (NavigationService.CurrentSource.OriginalString == "Popcorn;component/Pages/PlayerPage.xaml")
                    {
                        NavigationService.Refresh();
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("Popcorn;component/Pages/PlayerPage.xaml",
                            UriKind.Relative));
                    }

                    await Task.Delay(500);
                    IgnoreTaskbarOnMaximize = true;
                }));

            Messenger.Default.Register<PlayMediaMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                MediaPlayer = new MediaPlayerViewModel(_applicationService, message.MediaPath, message.MediaPath,
                    MediaType.Unkown,
                    () =>
                    {
                        Messenger.Default.Send(new StopPlayMediaMessage());
                    },
                    () =>
                    {
                        Messenger.Default.Send(new StopPlayMediaMessage());
                    },
                    message.BufferProgress,
                    message.BandwidthRate);

                ApplicationService.IsMediaPlaying = true;
                IsShowFlyoutOpen = false;
                IsMovieFlyoutOpen = false;
                if (NavigationService.CurrentSource.OriginalString == "Popcorn;component/Pages/PlayerPage.xaml")
                {
                    NavigationService.Refresh();
                }
                else
                {
                    NavigationService.Navigate(new Uri("Popcorn;component/Pages/PlayerPage.xaml", UriKind.Relative));
                }

                await Task.Delay(500);
                IgnoreTaskbarOnMaximize = true;
            }));

            Messenger.Default.Register<PlayMovieMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                MediaPlayer = new MediaPlayerViewModel(_applicationService, message.Movie.FilePath, message.Movie.Title,
                    MediaType.Movie,
                    () =>
                    {
                        Messenger.Default.Send(new StopPlayingMovieMessage());
                    },
                    async () =>
                    {
                        await _userService.SetMovieAsync(message.Movie);
                        Messenger.Default.Send(new ChangeSeenMovieMessage());
                        Messenger.Default.Send(new StopPlayingMovieMessage());
                    },
                    message.BufferProgress,
                    message.BandwidthRate,
                    message.Movie.SelectedSubtitle?.FilePath);

                ApplicationService.IsMediaPlaying = true;
                IsMovieFlyoutOpen = false;
                if (NavigationService.CurrentSource.OriginalString == "Popcorn;component/Pages/PlayerPage.xaml")
                {
                    NavigationService.Refresh();
                }
                else
                {
                    NavigationService.Navigate(new Uri("Popcorn;component/Pages/PlayerPage.xaml", UriKind.Relative));
                }

                await Task.Delay(500);
                IgnoreTaskbarOnMaximize = true;
            }));

            Messenger.Default.Register<PlayTrailerMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                MediaPlayer = new MediaPlayerViewModel(_applicationService, message.TrailerUrl, message.MovieTitle,
                    MediaType.Unkown,
                    message.TrailerStoppedAction, message.TrailerEndedAction);
                ApplicationService.IsMediaPlaying = true;
                IsMovieFlyoutOpen = false;
                IsShowFlyoutOpen = false;
                if (NavigationService.CurrentSource.OriginalString == "Popcorn;component/Pages/PlayerPage.xaml")
                {
                    NavigationService.Refresh();
                }
                else
                {
                    NavigationService.Navigate(new Uri("Popcorn;component/Pages/PlayerPage.xaml", UriKind.Relative));
                }

                await Task.Delay(500);
                IgnoreTaskbarOnMaximize = true;
            }));

            Messenger.Default.Register<StopPlayingTrailerMessage>(this, message =>
            {
                IgnoreTaskbarOnMaximize = false;
                if (ApplicationService.IsMediaPlaying)
                {
                    if (ApplicationService.IsFullScreen)
                    {
                        ApplicationService.IsFullScreen = false;
                        ApplicationService.IsFullScreen = true;
                    }

                    ApplicationService.IsMediaPlaying = false;
                    if (message.Type == MediaType.Movie)
                    {
                        IsMovieFlyoutOpen = true;
                    }
                    else
                    {
                        IsShowFlyoutOpen = true;
                    }
                }
            });

            Messenger.Default.Register<StopPlayMediaMessage>(this, message =>
            {
                IgnoreTaskbarOnMaximize = false;
                if (ApplicationService.IsMediaPlaying && ApplicationService.IsFullScreen)
                {
                    ApplicationService.IsFullScreen = false;
                    ApplicationService.IsFullScreen = true;
                }

                ApplicationService.IsMediaPlaying = false;
            });

            Messenger.Default.Register<StopPlayingEpisodeMessage>(
                this,
                message =>
                {
                    IgnoreTaskbarOnMaximize = false;
                    if (ApplicationService.IsMediaPlaying && ApplicationService.IsFullScreen)
                    {
                        ApplicationService.IsFullScreen = false;
                        ApplicationService.IsFullScreen = true;
                    }

                    ApplicationService.IsMediaPlaying = false;
                    IsShowFlyoutOpen = true;
                });

            Messenger.Default.Register<StopPlayingMovieMessage>(
                this,
                message =>
                {
                    IgnoreTaskbarOnMaximize = false;
                    if (ApplicationService.IsMediaPlaying && ApplicationService.IsFullScreen)
                    {
                        ApplicationService.IsFullScreen = false;
                        ApplicationService.IsFullScreen = true;
                    }

                    ApplicationService.IsMediaPlaying = false;
                    IsMovieFlyoutOpen = true;
                });

            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                message =>
                {
                    var pages = SimpleIoc.Default.GetInstance<PagesViewModel>();
                    foreach (var page in pages.Pages)
                    {
                        if (page is MoviePageViewModel)
                        {
                            page.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("MoviesLabel");
                        }
                        else if (page is ShowPageViewModel)
                        {
                            page.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("ShowsLabel");
                        }
                    }
                });

            Messenger.Default.Register<UnhandledExceptionMessage>(this, message => ManageException(message.Exception));

            _castMediaMessage = Messenger.Default.RegisterAsyncMessage<CastMediaMessage>(async message =>
            {
                var vm = new ChromecastDialogViewModel(message.Title, message.MediaPath, message.SubtitleFilePath, _fileServerService);
                var castDialog = new CastDialog
                {
                    DataContext = vm
                };
                var cts = new TaskCompletionSource<object>();
                vm.OnCloseAction = async () =>
                {
                    await _dialogCoordinator.HideMetroDialogAsync(this, castDialog);
                    message.Cancelled = true;
                    cts.TrySetResult(null);
                };
                await _dialogCoordinator.ShowMetroDialogAsync(this, castDialog);
                await cts.Task;
            });

            _customSubtitleMessage = Messenger.Default.RegisterAsyncMessage<CustomSubtitleMessage>(
                async message =>
                {
                    var fileDialog = new OpenFileDialog
                    {
                        Title = "Open Sub File",
                        Filter = "SUB files (*.sub,*srt,*sbv)|*.sub;*.srt;*.sbv",
                        InitialDirectory = @"C:\"
                    };

                    if (fileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            message.FileName = fileDialog.FileName;
                            await Task.FromResult(message.FileName);
                        }
                        catch (Exception)
                        {
                            message.Error = true;
                        }
                    }
                });
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            CloseMoviePageCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new StopPlayingTrailerMessage(MediaType.Movie));
                IsMovieFlyoutOpen = false;
                if (NavigationService.CurrentSource.OriginalString != "Pages/HomePage.xaml" && NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            });

            CloseShowPageCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new StopPlayingTrailerMessage(MediaType.Show));
                IsShowFlyoutOpen = false;
                if (NavigationService.CurrentSource.OriginalString != "Pages/HomePage.xaml" && NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            });

            MainWindowClosingCommand = new RelayCommand(ClearFolders);

            OpenSettingsCommand = new RelayCommand(() => IsSettingsFlyoutOpen = !IsSettingsFlyoutOpen);

            DropFileCommand = new RelayCommand<DragEventArgs>(async e =>
            {
                try
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                        var torrentFile = files?.FirstOrDefault(a => a.Contains("torrent"));
                        if (torrentFile != null)
                        {
                            var vm = new DropTorrentDialogViewModel(torrentFile);
                            var dropTorrentDialog = new DropTorrentDialog
                            {
                                DataContext = vm
                            };

                            await _dialogCoordinator.ShowMetroDialogAsync(this, dropTorrentDialog);
                            var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                            Task.Run(async () =>
                            {
                                await vm.Download(settings.UploadLimit, settings.DownloadLimit,
                                    async () =>
                                    {
                                        await _dialogCoordinator.HideMetroDialogAsync(this, dropTorrentDialog);
                                    }, async () =>
                                    {
                                        await _dialogCoordinator.HideMetroDialogAsync(this, dropTorrentDialog);
                                    });
                            });
                        }

                        Messenger.Default.Send(new DropFileMessage(DropFileMessage.DropFileEvent.Leave));
                    }
                }
                catch (Exception)
                {
                    Messenger.Default.Send(
                        new UnhandledExceptionMessage(
                            new PopcornException("An issue has occured while processing the dropped file.")));
                }
            });

            OpenAboutCommand = new RelayCommand(async () =>
            {
                var aboutDialog = new AboutDialog();
                var vm = new AboutDialogViewModel(async () =>
                {
                    await _dialogCoordinator.HideMetroDialogAsync(this, aboutDialog);
                });

                aboutDialog.DataContext = vm;
                await _dialogCoordinator.ShowMetroDialogAsync(this, aboutDialog);
            });

            OpenHelpCommand = new RelayCommand(async () =>
            {
                var helpDialog = new HelpDialog();
                var vm = new HelpDialogViewModel(async () =>
                {
                    await _dialogCoordinator.HideMetroDialogAsync(this, helpDialog);
                });

                helpDialog.DataContext = vm;
                await _dialogCoordinator.ShowMetroDialogAsync(this, helpDialog);
            });

            DragEnterFileCommand = new RelayCommand<DragEventArgs>(e =>
            {
                Messenger.Default.Send(new DropFileMessage(DropFileMessage.DropFileEvent.Enter));
            });

            DragLeaveFileCommand = new RelayCommand<DragEventArgs>(e =>
            {
                Messenger.Default.Send(new DropFileMessage(DropFileMessage.DropFileEvent.Leave));
            });

            InitializeAsyncCommand = new RelayCommand(async () =>
            {
                var cmd = Environment.GetCommandLineArgs();
                if (cmd.Any() && cmd.Contains("restart"))
                {
                    OpenAboutCommand.Execute(null);
                }
#if !DEBUG
                await StartUpdateProcessAsync();
#endif
            });
        }

        /// <summary>
        /// Clear download folders
        /// </summary>
        private void ClearFolders()
        {
            if (Directory.Exists(Constants.Subtitles))
            {
                FileHelper.DeleteFolder(Constants.Subtitles);
            }

            if (Directory.Exists(Constants.MovieDownloads))
            {
                FileHelper.DeleteFolder(Constants.MovieDownloads);
            }

            if (Directory.Exists(Constants.DropFilesDownloads))
            {
                FileHelper.DeleteFolder(Constants.DropFilesDownloads);
            }

            if (Directory.Exists(Constants.ShowDownloads))
            {
                FileHelper.DeleteFolder(Constants.ShowDownloads);
            }

            if (Directory.Exists(Constants.MovieTorrentDownloads))
            {
                FileHelper.DeleteFolder(Constants.MovieTorrentDownloads);
            }
        }

        /// <summary>
        /// Look for update then download and apply if any
        /// </summary>
        private async Task StartUpdateProcessAsync()
        {
            var watchStart = Stopwatch.StartNew();

            Logger.Info(
                "Looking for updates...");
            try
            {
                using (var updateManager = await UpdateManager.GitHubUpdateManager(Constants.GithubRepository))
                {
                    var updateInfo = await updateManager.CheckForUpdate();
                    if (updateInfo == null)
                    {
                        Logger.Error(
                            "Problem while trying to check new updates.");
                        return;
                    }

                    if (updateInfo.ReleasesToApply.Any())
                    {
                        Logger.Info(
                            $"A new update has been found!\n Currently installed version: {updateInfo.CurrentlyInstalledVersion?.Version?.Version.Major}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Minor}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Build} - New update: {updateInfo.FutureReleaseEntry?.Version?.Version.Major}.{updateInfo.FutureReleaseEntry?.Version?.Version.Minor}.{updateInfo.FutureReleaseEntry?.Version?.Version.Build}");

                        await updateManager.DownloadReleases(updateInfo.ReleasesToApply, x => Logger.Info(
                            $"Downloading new update... {x}%"));

                        var latestExe = await updateManager.ApplyReleases(updateInfo, x => Logger.Info(
                            $"Applying... {x}%"));

                        Logger.Info(
                            "A new update has been applied.");

                        var releaseInfos = string.Empty;
                        foreach (var releaseInfo in updateInfo.FetchReleaseNotes())
                        {
                            var info = releaseInfo.Value;

                            var pFrom = info.IndexOf("<p>", StringComparison.InvariantCulture) + "<p>".Length;
                            var pTo = info.LastIndexOf("</p>", StringComparison.InvariantCulture);

                            releaseInfos = string.Concat(releaseInfos, info.Substring(pFrom, pTo - pFrom),
                                Environment.NewLine);
                        }

                        var updateDialog =
                            new UpdateDialog(
                                new UpdateDialogSettings(
                                    LocalizationProviderHelper.GetLocalizedValue<string>("NewUpdateLabel"),
                                    LocalizationProviderHelper.GetLocalizedValue<string>("NewUpdateDescriptionLabel"),
                                    releaseInfos));
                        await _dialogCoordinator.ShowMetroDialogAsync(this, updateDialog);
                        var updateDialogResult = await updateDialog.WaitForButtonPressAsync();
                        await _dialogCoordinator.HideMetroDialogAsync(this, updateDialog);

                        if (!updateDialogResult.Restart) return;

                        Logger.Info(
                            "Restarting...");

                        Process.Start($@"{latestExe}\Popcorn.exe", "restart");
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        Logger.Info(
                            "No update available.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Something went wrong when trying to update app. {ex.Message}");
            }

            watchStart.Stop();
            var elapsedStartMs = watchStart.ElapsedMilliseconds;
            Logger.Info(
                $"Finished looking for updates in {elapsedStartMs}.");
        }

        /// <summary>
        /// Display a dialog on unhandled exception
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Fatal(ex);
                ManageException(
                    new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("FatalError")));
            }
        }

        /// <summary>
        /// Manage an exception
        /// </summary>
        /// <param name="exception">The exception to manage</param>
        private void ManageException(Exception exception)
        {
            if (_isManagingException)
                return;

            _isManagingException = true;
            IsMovieFlyoutOpen = false;
            IsSettingsFlyoutOpen = false;

            if (exception is WebException || exception is SocketException)
                _applicationService.IsConnectionInError = true;

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                var exceptionDialog =
                    new ExceptionDialog(
                        new ExceptionDialogSettings(
                            LocalizationProviderHelper.GetLocalizedValue<string>("EmbarrassingError"),
                            exception.Message));
                await _dialogCoordinator.ShowMetroDialogAsync(this, exceptionDialog);
                await exceptionDialog.WaitForButtonPressAsync();
                _isManagingException = false;
                await _dialogCoordinator.HideMetroDialogAsync(this, exceptionDialog);
            });
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
                _customSubtitleMessage?.Dispose();
                _castMediaMessage?.Dispose();
            }

            _disposed = true;
        }
    }
}