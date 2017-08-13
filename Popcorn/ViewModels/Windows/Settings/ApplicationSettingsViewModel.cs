using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.Localization;
using Popcorn.Services.Subtitles;
using Popcorn.Services.User;
using Popcorn.Utils;

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
        /// Initializes a new instance of the ApplicationSettingsViewModel class.
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="subtitlesService">Subtitles service</param>
        public ApplicationSettingsViewModel(IUserService userService, ISubtitlesService subtitlesService)
        {
            _userService = userService;
            Version = Constants.AppVersion;

            RefreshCacheSize();
            RegisterCommands();
            SubtitlesColor = Color.FromRgb(255, 255, 255);

            Task.Run(async () =>
            {
                try
                {
                    DownloadLimit = await _userService.GetDownloadLimit();
                    UploadLimit = await _userService.GetUploadLimit();
                    var defaultSubtitleLanguage = await _userService.GetDefaultSubtitleLanguage();
                    DefaultHdQuality = await _userService.GetDefaultHdQuality();
                    AvailableSubtitlesLanguages = new ObservableRangeCollection<string>();
                    var languages = (await subtitlesService.GetSubLanguages()).Select(a => a.LanguageName)
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
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
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
                Task.Run(async () =>
                {
                    await _userService.SetDownloadLimit(value);
                });
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
                Task.Run(async () =>
                {
                    await _userService.SetDefaultSubtitleLanguage(_defaultSubtitleLanguage);
                });
            }
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
                Task.Run(async () =>
                {
                    await _userService.SetDefaultHdQuality(value);
                });
            }
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
                Task.Run(async () =>
                {
                    await _userService.SetUploadLimit(value);
                });
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
        /// Command used to initialize the settings asynchronously
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Clear the cache
        /// </summary>
        public RelayCommand ClearCacheCommand { get; private set; }

        /// <summary>
        /// Update size cache
        /// </summary>
        public RelayCommand UpdateCacheSizeCommand { get; private set; }

        /// <summary>
        /// Subtitles color
        /// </summary>
        public Color SubtitlesColor
        {
            get => _subtitlesColor;
            set { Set(() => SubtitlesColor, ref _subtitlesColor, value); }
        }

        /// <summary>
        /// Load asynchronously the languages of the application
        /// </summary>
        private async Task InitializeAsync()
        {
            Language = new Language(_userService);
            await Language.LoadLanguages();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            InitializeAsyncCommand = new RelayCommand(async () => await InitializeAsync());
            UpdateCacheSizeCommand = new RelayCommand(RefreshCacheSize);
            ClearCacheCommand = new RelayCommand(() =>
            {
                FileHelper.DeleteFolder(Constants.Assets);
                RefreshCacheSize();
            });
        }

        /// <summary>
        /// Refresh cache size
        /// </summary>
        private void RefreshCacheSize()
        {
            var cache = FileHelper.GetDirectorySize(Constants.Assets);
            CacheSize =
                (cache / 1024 / 1024)
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}