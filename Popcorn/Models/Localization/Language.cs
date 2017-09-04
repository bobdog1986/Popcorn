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
        private User.Language _currentLanguage;

        /// <summary>
        /// Available languages
        /// </summary>
        private ICollection<User.Language> _languages;

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
        public ICollection<User.Language> Languages
        {
            get => _languages;
            set { Set(() => Languages, ref _languages, value); }
        }

        /// <summary>
        /// Current language used in the application
        /// </summary>
        public User.Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                Set(() => CurrentLanguage, ref _currentLanguage, value);
                _userService.SetCurrentLanguage(value);
            }
        }

        /// <summary>
        /// Load languages
        /// </summary>
        public void LoadLanguages()
        {
            CurrentLanguage = _userService.GetCurrentLanguage();
            Languages = _userService.GetAvailableLanguages();
        }
    }
}