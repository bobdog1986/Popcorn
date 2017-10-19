using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Enterwell.Clients.Wpf.Notifications;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using Popcorn.ColorPicker;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.Localization;
using Popcorn.Models.Subtitles;
using Popcorn.Services.Associations;
using Popcorn.Services.Cache;
using Popcorn.Services.Subtitles;
using Popcorn.Services.Trakt;
using Popcorn.Services.User;
using Popcorn.Utils;
using Squirrel;

namespace Popcorn.ViewModels.Windows.Settings
{
    /// <summary>
    /// Application's settings
    /// </summary>
    public sealed class ApplicationSettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Services used to interacts with languages
        /// </summary>
        private readonly IUserService _userService;

        /// <summary>
        /// Subtitle service
        /// </summary>
        private readonly ISubtitlesService _subtitlesService;

        /// <summary>
        /// Trakt service
        /// </summary>
        private readonly ITraktService _traktService;

        /// <summary>
        /// File association service
        /// </summary>
        private readonly IFileAssociationService _fileAssociationService;

        /// <summary>
        /// The download limit
        /// </summary>
        private int _downloadLimit;

        /// <summary>
        /// The language used through the application
        /// </summary>
        private Language _language;

        /// <summary>
        /// The upload limit
        /// </summary>
        private int _uploadLimit;

        /// <summary>
        /// The version of the app
        /// </summary>
        private string _version;

        /// <summary>
        /// Cache size
        /// </summary>
        private string _cacheSize;

        /// <summary>
        /// Default quality
        /// </summary>
        private bool _defaultHdQuality;

        /// <summary>
        /// Available languages for subtitles
        /// </summary>
        private ObservableRangeCollection<string> _availableSubtitlesLanguages;

        /// <summary>
        /// Default subtitle language
        /// </summary>
        private string _defaultSubtitleLanguage;

        /// <summary>
        /// Subtitles color
        /// </summary>
        private Color _subtitlesColor;

        /// <summary>
        /// True if subtitles are loading
        /// </summary>
        private bool _loadingSubtitles;

        /// <summary>
        /// The available subtitle sizes
        /// </summary>
        private ObservableCollection<SubtitleSize> _subtitleSizes;

        /// <summary>
        /// The current subtitle size
        /// </summary>
        private SubtitleSize _selectedSubtitleSize;

        /// <summary>
        /// Command used to show the Trakt dialog
        /// </summary>
        private ICommand _showTraktDialogCommand;

        /// <summary>
        /// Command used to change subtitle color
        /// </summary>
        private ICommand _changeSubtitleColorCommand;

        /// <summary>
        /// Command used to logout from Trakt
        /// </summary>
        private ICommand _logoutTraktCommand;

        /// <summary>
        /// If an update is available
        /// </summary>
        private bool _updateAvailable;

        /// <summary>
        /// If an update is downloading
        /// </summary>
        private bool _updateDownloading;

        /// <summary>
        /// If torrent file association is enabled
        /// </summary>
        private bool _torrentFileAssociationEnabled;

        /// <summary>
        /// If magnet link association is enabled
        /// </summary>
        private bool _magnetLinkAssociationEnabled;

        /// <summary>
        /// If an update is applying
        /// </summary>
        private bool _updateApplying;

        /// <summary>
        /// Update download progress
        /// </summary>
        private int _updateDownloadProgress;

        /// <summary>
        /// Update apply progress
        /// </summary>
        private int _updateApplyProgress;

        /// <summary>
        /// If an update has been applied
        /// </summary>
        private bool _updateApplied;

        /// <summary>
        /// File path of the installed update
        /// </summary>
        private string _updateFilePath;

        /// <summary>
        /// Notification manager
        /// </summary>
        private readonly NotificationMessageManager _manager;

        /// <summary>
        /// True if Trakt is connected
        /// </summary>
        private bool _isTraktLoggedIn;

        /// <summary>
        /// The cache service
        /// </summary>
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the ApplicationSettingsViewModel class.
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="subtitlesService">Subtitles service</param>
        /// <param name="traktService">Trakt service</param>
        /// <param name="cacheService">Cache service</param>
        /// <param name="fileAssociationService">File association service</param>
        /// <param name="manager">Notification manager</param>
        public ApplicationSettingsViewModel(IUserService userService, ISubtitlesService subtitlesService, ITraktService traktService, ICacheService cacheService, IFileAssociationService fileAssociationService, NotificationMessageManager manager)
        {
            _fileAssociationService = fileAssociationService;
            _cacheService = cacheService;
            _traktService = traktService;
            _manager = manager;
            _userService = userService;
            _subtitlesService = subtitlesService;
            Version = Constants.AppVersion;
            RegisterCommands();
        }

        /// <summary>
        /// The download limit
        /// </summary>
        public int DownloadLimit
        {
            get => _downloadLimit;
            set
            {
                Set(() => DownloadLimit, ref _downloadLimit, value);
                _userService.SetDownloadLimit(value);
            }
        }
        
        /// <summary>
        /// Default subtitle language
        /// </summary>
        public string DefaultSubtitleLanguage
        {
            get => _defaultSubtitleLanguage;
            set
            {
                Set(() => DefaultSubtitleLanguage, ref _defaultSubtitleLanguage, value);
                _userService.SetDefaultSubtitleLanguage(_defaultSubtitleLanguage);
            }
        }

        /// <summary>
        /// Selected subtitle size
        /// </summary>
        public SubtitleSize SelectedSubtitleSize
        {
            get => _selectedSubtitleSize;
            set
            {
                Set(ref _selectedSubtitleSize, value);
                _userService.SetDefaultSubtitleSize(value);
            }
        }

        /// <summary>
        /// Show Trakt dialog
        /// </summary>
        public ICommand ShowTraktDialogCommand
        {
            get => _showTraktDialogCommand;
            set => Set(ref _showTraktDialogCommand, value);
        }

        /// <summary>
        /// Available subtitle sizes
        /// </summary>
        public ObservableCollection<SubtitleSize> SubtitleSizes
        {
            get => _subtitleSizes;
            set => Set(ref _subtitleSizes, value);
        }

        /// <summary>
        /// Available languages for subtitles
        /// </summary>
        public ObservableRangeCollection<string> AvailableSubtitlesLanguages
        {
            get => _availableSubtitlesLanguages;
            set { Set(() => AvailableSubtitlesLanguages, ref _availableSubtitlesLanguages, value); }
        }

        /// <summary>
        /// Default quality
        /// </summary>
        public bool DefaultHdQuality
        {
            get => _defaultHdQuality;
            set
            {
                Set(() => DefaultHdQuality, ref _defaultHdQuality, value);
                _userService.SetDefaultHdQuality(value);
            }
        }

        /// <summary>
        /// If subtitles are loading
        /// </summary>
        public bool LoadingSubtitles
        {
            get => _loadingSubtitles;
            set { Set(() => LoadingSubtitles, ref _loadingSubtitles, value); }
        }

        /// <summary>
        /// The version of the app
        /// </summary>
        public string Version
        {
            get => _version;
            set { Set(() => Version, ref _version, value); }
        }

        /// <summary>
        /// Cache size
        /// </summary>
        public string CacheSize
        {
            get => _cacheSize;
            set { Set(() => CacheSize, ref _cacheSize, value); }
        }

        /// <summary>
        /// The upload limit
        /// </summary>
        public int UploadLimit
        {
            get => _uploadLimit;
            set
            {
                Set(() => UploadLimit, ref _uploadLimit, value);
                _userService.SetUploadLimit(value);
            }
        }

        /// <summary>
        /// True if update is downloading
        /// </summary>
        public bool UpdateDownloading
        {
            get => _updateDownloading;
            set
            {
                Set(() => UpdateDownloading, ref _updateDownloading, value);
            }
        }

        /// <summary>
        /// True if torrent file association is enabled
        /// </summary>
        public bool TorrentFileAssociationEnabled
        {
            get => _torrentFileAssociationEnabled;
            set
            {
                Set(() => TorrentFileAssociationEnabled, ref _torrentFileAssociationEnabled, value);
                _userService.SetTorrentFileAssociation(value);
                if (!value)
                {
                    _fileAssociationService.UnregisterTorrentFileAssociation();
                }
                else
                {
                    _fileAssociationService.RegisterTorrentFileAssociation();
                }
            }
        }

        /// <summary>
        /// True if magnet link association is enabled
        /// </summary>
        public bool MagnetLinkAssociationEnabled
        {
            get => _magnetLinkAssociationEnabled;
            set
            {
                Set(() => MagnetLinkAssociationEnabled, ref _magnetLinkAssociationEnabled, value);
                _userService.SetMagnetLinkAssociation(value);
                if (!value)
                {
                    _fileAssociationService.UnregisterMagnetLinkAssociation();
                }
                else
                {
                    _fileAssociationService.RegisterMagnetLinkAssociation();
                }
            }
        }

        /// <summary>
        /// True if update is available
        /// </summary>
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                Set(() => UpdateAvailable, ref _updateAvailable, value);
            }
        }

        /// <summary>
        /// True if update is applying
        /// </summary>
        public bool UpdateApplying
        {
            get => _updateApplying;
            set
            {
                Set(() => UpdateApplying, ref _updateApplying, value);
            }
        }

        /// <summary>
        /// True if update has been applied
        /// </summary>
        public bool UpdateApplied
        {
            get => _updateApplied;
            set
            {
                Set(() => UpdateApplied, ref _updateApplied, value);
            }
        }

        /// <summary>
        /// The update download progress
        /// </summary>
        public int UpdateDownloadProgress
        {
            get => _updateDownloadProgress;
            set
            {
                Set(() => UpdateDownloadProgress, ref _updateDownloadProgress, value);
            }
        }

        /// <summary>
        /// The update apply progress
        /// </summary>
        public int UpdateApplyProgress
        {
            get => _updateApplyProgress;
            set
            {
                Set(() => UpdateApplyProgress, ref _updateApplyProgress, value);
            }
        }

        /// <summary>
        /// The language used through the application
        /// </summary>
        public Language Language
        {
            get => _language;
            set { Set(() => Language, ref _language, value); }
        }

        /// <summary>
        /// True if Trakt is connected
        /// </summary>
        public bool IsTraktLoggedIn
        {
            get => _isTraktLoggedIn;
            set { Set(() => IsTraktLoggedIn, ref _isTraktLoggedIn, value); }
        }

        /// <summary>
        /// Clear the cache
        /// </summary>
        public RelayCommand ClearCacheCommand { get; private set; }

        /// <summary>
        /// Command used to change cache location
        /// </summary>
        public RelayCommand ChangeCacheLocationCommand { get; private set; }

        /// <summary>
        /// Update size cache
        /// </summary>
        public RelayCommand UpdateCacheSizeCommand { get; private set; }
        
        /// <summary>
        /// Change subtitle
        /// </summary>
        public ICommand ChangeSubtitleColorCommand
        {
            get => _changeSubtitleColorCommand;
            set => Set(ref _changeSubtitleColorCommand, value);
        }

        /// <summary>
        /// Change subtitle
        /// </summary>
        public ICommand LogoutTraktCommand
        {
            get => _logoutTraktCommand;
            set => Set(ref _logoutTraktCommand, value);
        }

        /// <summary>
        /// Subtitles color
        /// </summary>
        public Color SubtitlesColor
        {
            get => _subtitlesColor;
            set
            {
                Set(ref _subtitlesColor, value);
                _userService.SetDefaultSubtitleColor("#" + _subtitlesColor.R.ToString("X2") +
                                                     _subtitlesColor.G.ToString("X2") +
                                                     _subtitlesColor.B.ToString("X2"));
            }
        }

        /// <summary>
        /// Load asynchronously the languages of the application
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var user = await _userService.GetUser().ConfigureAwait(false);
                FileHelper.CreateFolders();
                RefreshCacheSize();
                SubtitleSizes = new ObservableCollection<SubtitleSize>
                {
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Bigger"),
                        Size = 6
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Big"),
                        Size = 12
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Normal"),
                        Size = 16
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Small"),
                        Size = 18
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Smaller"),
                        Size = 20
                    }
                };

                DownloadLimit = user.DownloadLimit;
                UploadLimit = user.UploadLimit;
                var defaultSubtitleLanguage = user.DefaultSubtitleLanguage;
                var subtitleSize = user.DefaultSubtitleSize;
                DefaultHdQuality = user.DefaultHdQuality;
                TorrentFileAssociationEnabled = user.EnableTorrentFileAssociation;
                MagnetLinkAssociationEnabled = user.EnableMagnetLinkAssociation;
                if (TorrentFileAssociationEnabled)
                {
                    _fileAssociationService.RegisterTorrentFileAssociation();
                }
                else if (!TorrentFileAssociationEnabled && _fileAssociationService.TorrentFileAssociationIsEnabled())
                {
                    _fileAssociationService.UnregisterTorrentFileAssociation();
                }

                if (MagnetLinkAssociationEnabled)
                {
                    _fileAssociationService.RegisterMagnetLinkAssociation();
                }
                else if (!MagnetLinkAssociationEnabled && _fileAssociationService.MagneLinkAssociationIsEnabled())
                {
                    _fileAssociationService.UnregisterMagnetLinkAssociation();
                }

                SelectedSubtitleSize = SubtitleSizes.FirstOrDefault(a => a.Size == subtitleSize.Size);
                SubtitlesColor =
                    (Color)ColorConverter.ConvertFromString(user.DefaultSubtitleColor);

#pragma warning disable CS4014
                Task.Run(async () =>
                {
                    IsTraktLoggedIn = await _traktService.IsLoggedIn();
                    LoadingSubtitles = true;
                    AvailableSubtitlesLanguages = new ObservableRangeCollection<string>();
                    var languages = (await _subtitlesService.GetSubLanguages().ConfigureAwait(false)).Select(a => a.LanguageName)
                        .OrderBy(a => a)
                        .ToList();
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        languages.Insert(0,
                            LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"));
                        AvailableSubtitlesLanguages.AddRange(
                            new ObservableRangeCollection<string>(languages));
                        DefaultSubtitleLanguage = AvailableSubtitlesLanguages.Any(a => a == defaultSubtitleLanguage)
                            ? defaultSubtitleLanguage
                            : LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel");
                        LoadingSubtitles = false;
                    });

#if !DEBUG
                await StartUpdateProcessAsync().ConfigureAwait(false);
#endif
                });
#pragma warning restore CS4014
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    LoadingSubtitles = false;
                    AvailableSubtitlesLanguages.Insert(0,
                        LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"));
                    DefaultSubtitleLanguage = AvailableSubtitlesLanguages.FirstOrDefault();
                });
            }

            Language = new Language(_userService);
            Language.LoadLanguages();
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
                        Messenger.Default.Send(new UpdateAvailableMessage());
                        UpdateAvailable = true;
                        Logger.Info(
                            $"A new update has been found!\n Currently installed version: {updateInfo.CurrentlyInstalledVersion?.Version?.Version.Major}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Minor}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Build} - New update: {updateInfo.FutureReleaseEntry?.Version?.Version.Major}.{updateInfo.FutureReleaseEntry?.Version?.Version.Minor}.{updateInfo.FutureReleaseEntry?.Version?.Version.Build}");

                        UpdateDownloading = true;
                        await updateManager.DownloadReleases(updateInfo.ReleasesToApply, progress =>
                        {
                            UpdateDownloadProgress = progress;
                        });
                        UpdateDownloading = false;
                        UpdateApplying = true;
                        _updateFilePath = await updateManager.ApplyReleases(updateInfo, progress =>
                        {
                            UpdateApplyProgress = progress;
                        });
                        UpdateApplying = false;
                        UpdateApplied = true;
                        Logger.Info(
                            "A new update has been applied.");
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            _manager.CreateMessage()
                                .Accent("#1751C3")
                                .Background("#333")
                                .HasBadge("Info")
                                .HasMessage(LocalizationProviderHelper.GetLocalizedValue<string>("UpdateApplied"))
                                .Dismiss().WithButton(LocalizationProviderHelper.GetLocalizedValue<string>("Restart"),
                                    button =>
                                    {
                                        Logger.Info(
                                            "Restarting...");

                                        Process.Start($@"{_updateFilePath}\Popcorn.exe", "restart");
                                        Application.Current.MainWindow.Close();
                                    })
                                .Dismiss().WithButton(LocalizationProviderHelper.GetLocalizedValue<string>("LaterLabel"),
                                    button => { })
                                .Queue();
                        });
                    }
                    else
                    {
                        UpdateAvailable = false;
                        Logger.Info(
                            "No update available.");
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
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            UpdateCacheSizeCommand = new RelayCommand(RefreshCacheSize);
            ClearCacheCommand = new RelayCommand(() =>
            {
                FileHelper.DeleteFolder(_cacheService.Assets);
                RefreshCacheSize();
            });

            ShowTraktDialogCommand = new RelayCommand(async () =>
            {
                var message = new ShowTraktDialogMessage();
                await Messenger.Default.SendAsync(message);
                IsTraktLoggedIn = message.IsLoggedIn ?? false;
            });

            ChangeSubtitleColorCommand = new RelayCommand<EventArgs<Color>>(args =>
            {
                SubtitlesColor = args.Value;
            });

            LogoutTraktCommand = new RelayCommand(async () =>
            {
                await _traktService.Logout();
                IsTraktLoggedIn = false;
            });

            ChangeCacheLocationCommand = new RelayCommand(() =>
            {
                try
                {
                    var dialog = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true,
                        InitialDirectory = _userService.GetCacheLocationPath(),
                        AddToMostRecentlyUsedList = false,
                        AllowNonFileSystemItems = false,
                        DefaultDirectory = _userService.GetCacheLocationPath(),
                        EnsureFileExists = true,
                        EnsurePathExists = true,
                        EnsureReadOnly = false,
                        EnsureValidNames = true,
                        Multiselect = false,
                        ShowPlacesList = true
                    };

                    var result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        FileHelper.ClearFolders(true);
                        _userService.SetCacheLocationPath(dialog.FileName);
                        FileHelper.CreateFolders();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        /// <summary>
        /// Refresh cache size
        /// </summary>
        private void RefreshCacheSize()
        {
            var cache = FileHelper.GetDirectorySize(_cacheService.Assets);
            CacheSize =
                (cache / 1024 / 1024)
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}