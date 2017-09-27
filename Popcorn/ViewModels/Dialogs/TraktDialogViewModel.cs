using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Akavache;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using NLog;
using Popcorn.Services.Trakt;
using TraktApiSharp.Authentication;

namespace Popcorn.ViewModels.Dialogs
{
    public class TraktDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        public readonly ITraktService _traktService;

        private readonly TraktAuthorization _traktAuthorization;

        private ICommand _initializeAsyncCommand;

        private ICommand _closeCommand;

        private string _traktOAuthUrl;

        private bool _isLoggedIn;

        private bool _isLoading;

        public TraktDialogViewModel(ITraktService traktService)
        {
            _traktService = traktService;
            CloseCommand = new RelayCommand(() =>
            {
                CloseAction.Invoke();
            });

            InitializeAsyncCommand = new RelayCommand(async () =>
            {
                try
                {
                    IsLoading = true;
                    IsLoggedIn = await _traktService.IsLoggedIn();
                    TraktOAuthUrl = !IsLoggedIn ? _traktService.GetAuthorizationUrl() : string.Empty;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        public async Task ValidateOAuthCode(string code)
        {
            try
            {
                IsLoading = true;
                await _traktService.AuthorizeAsync(code);
                IsLoggedIn = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsLoading = false;
                CloseAction.Invoke();
            }
        }

        public Action CloseAction;

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set { Set(ref _closeCommand, value); }
        }

        public ICommand InitializeAsyncCommand
        {
            get => _initializeAsyncCommand;
            set { Set(ref _initializeAsyncCommand, value); }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set { Set(ref _isLoggedIn, value); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { Set(ref _isLoading, value); }
        }

        public string TraktOAuthUrl
        {
            get => _traktOAuthUrl;
            set { Set(ref _traktOAuthUrl, value); }
        }
    }
}
