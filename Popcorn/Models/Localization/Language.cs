using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using NLog;
using Popcorn.Models.User;
using Popcorn.Services.User;

namespace Popcorn.Models.Localization
{
    /// <summary>
    /// Language
    /// </summary>
    public class Language : ObservableObject
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
        /// Current language
        /// </summary>
        private LanguageJson _currentLanguage;

        /// <summary>
        /// Available languages
        /// </summary>
        private ICollection<LanguageJson> _languages;

        /// <summary>
        /// Initialize a new instance of Language
        /// </summary>
        public Language(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Available languages of the application
        /// </summary>
        public ICollection<LanguageJson> Languages
        {
            get { return _languages; }
            set { Set(() => Languages, ref _languages, value); }
        }

        /// <summary>
        /// Current language used in the application
        /// </summary>
        public LanguageJson CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                Task.Run(async () => { await _userService.SetCurrentLanguageAsync(value); });
                Set(() => CurrentLanguage, ref _currentLanguage, value);
            }
        }

        /// <summary>
        /// Load languages
        /// </summary>
        public async Task LoadLanguages()
        {
            try
            {
                var watchStart = Stopwatch.StartNew();

                CurrentLanguage = await _userService.GetCurrentLanguageAsync();
                Languages = _userService.GetAvailableLanguages();

                watchStart.Stop();
                var elapsedLanguageMs = watchStart.ElapsedMilliseconds;
                Logger.Info(
                    "Languages loaded in {0} milliseconds.", elapsedLanguageMs);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}