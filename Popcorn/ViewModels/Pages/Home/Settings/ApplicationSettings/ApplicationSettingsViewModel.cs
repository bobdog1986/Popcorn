using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using ColorPicker;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using Popcorn.Helpers;
using Popcorn.Models.Subtitles;
using Popcorn.Models.User;
using Popcorn.Services.Cache;
using Popcorn.Services.Subtitles;
using Popcorn.Services.User;

namespace Popcorn.ViewModels.Pages.Home.Settings.ApplicationSettings
{
    public class ApplicationSettingsViewModel : ViewModelBase, IPageViewModel
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
        /// <see cref="Caption"/>
        /// </summary>
        private string _caption;

        /// <summary>
        /// The download limit
        /// </summary>
        private int _downloadLimit;

        /// <summary>
        /// The language used through the application
        /// </summary>
        private ObservableCollection<Language> _availableLanguages;

        /// <summary>
        /// The selected language
        /// </summary>
        private Language _selectedLanguage;

        /// <summary>
        /// The upload limit
        /// </summary>
        private int _uploadLimit;

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
        /// Command used to change subtitle color
        /// </summary>
        private ICommand _changeSubtitleColorCommand;

        /// <summary>
        /// The cache service
        /// </summary>
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the ApplicationSettingsViewModel class.
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="subtitlesService">Subtitles service</param>
        /// <param name="cacheService">Cache service</param>
        public ApplicationSettingsViewModel(IUserService userService, ISubtitlesService subtitlesService,
            ICacheService cacheService)
        {
            _cacheService = cacheService;
            _userService = userService;
            _subtitlesService = subtitlesService;
            RegisterCommands();
        }

        /// <summary>
        /// Tab caption 
        /// </summary>
        public string Caption
        {
            get => _caption;
            set => Set(ref _caption, value);
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
        /// The language used through the application
        /// </summary>
        public ObservableCollection<Language> AvailableLanguages
        {
            get => _availableLanguages;
            set { Set(() => AvailableLanguages, ref _availableLanguages, value); }
        }

        /// <summary>
        /// The selected language
        /// </summary>
        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                Set(() => SelectedLanguage, ref _selectedLanguage, value);
                _userService.SetCurrentLanguage(value);
            }
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
                var user = await _userService.GetUser();
                FileHelper.CreateFolders();
                RefreshCacheSize();
                SubtitleSizes = new ObservableCollection<SubtitleSize>
                {
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Bigger"),
                        Size = 34
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Big"),
                        Size = 30
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Normal"),
                        Size = 26
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Small"),
                        Size = 22
                    },
                    new SubtitleSize
                    {
                        Label = LocalizationProviderHelper.GetLocalizedValue<string>("Smaller"),
                        Size = 16
                    }
                };

                DownloadLimit = user.DownloadLimit;
                UploadLimit = user.UploadLimit;
                var defaultSubtitleLanguage = user.DefaultSubtitleLanguage;
                var subtitleSize = user.DefaultSubtitleSize;
                DefaultHdQuality = user.DefaultHdQuality;
                SelectedSubtitleSize = SubtitleSizes.FirstOrDefault(a => a.Size == subtitleSize.Size);
                SubtitlesColor =
                    (Color) ColorConverter.ConvertFromString(user.DefaultSubtitleColor);

                LoadingSubtitles = true;
                AvailableSubtitlesLanguages = new ObservableRangeCollection<string>();
                var languages = (await _subtitlesService.GetSubLanguages())
                    .Select(a => a.LanguageName)
                    .OrderBy(a => a)
                    .ToList();
                languages.Insert(0,
                    LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"));
                AvailableSubtitlesLanguages.AddRange(
                    new ObservableRangeCollection<string>(languages));
                DefaultSubtitleLanguage = AvailableSubtitlesLanguages.Any(a => a == defaultSubtitleLanguage)
                    ? defaultSubtitleLanguage
                    : LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel");
                LoadingSubtitles = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                LoadingSubtitles = false;
                AvailableSubtitlesLanguages.Insert(0,
                    LocalizationProviderHelper.GetLocalizedValue<string>("NoneLabel"));
                DefaultSubtitleLanguage = AvailableSubtitlesLanguages.FirstOrDefault();
            }

            AvailableLanguages = new ObservableCollection<Language>(_userService.GetAvailableLanguages());
            SelectedLanguage = _userService.GetCurrentLanguage();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            UpdateCacheSizeCommand = new RelayCommand(RefreshCacheSize);
            ClearCacheCommand = new RelayCommand(() =>
            {
                FileHelper.ClearFolders(true);
                RefreshCacheSize();
            });

            ChangeSubtitleColorCommand = new RelayCommand<EventArgs<Color>>(args => { SubtitlesColor = args.Value; });

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
            var cache = FileHelper.GetDirectorySize(_cacheService.Assets) +
                        FileHelper.GetDirectorySize(_cacheService.DropFilesDownloads) +
                        FileHelper.GetDirectorySize(_cacheService.MovieDownloads) +
                        FileHelper.GetDirectorySize(_cacheService.MovieTorrentDownloads) +
                        FileHelper.GetDirectorySize(_cacheService.PopcornTemp) +
                        FileHelper.GetDirectorySize(_cacheService.ShowDownloads) +
                        FileHelper.GetDirectorySize(_cacheService.Subtitles);
            CacheSize =
                $"{(cache / 1024 / 1024).ToString(CultureInfo.InvariantCulture)} MB";
        }
    }
}