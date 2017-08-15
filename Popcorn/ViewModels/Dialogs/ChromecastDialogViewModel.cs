using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Chromecast;
using Popcorn.Chromecast.Interfaces;
using Popcorn.Chromecast.Models;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Utils.Exceptions;

namespace Popcorn.ViewModels.Dialogs
{
    public class ChromecastDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private bool _loadingChromecasts;

        private ObservableCollection<ChromecastReceiver> _chromecasts;

        private ICommand _closeCommand;

        private ICommand _chooseChromecastDeviceCommand;

        private ICommand _refreshCommand;

        private bool _anyChromecast;

        private bool _connectedToChromecast;

        public Action OnCloseAction { get; set; }

        private bool _connectingToChromecast;

        private readonly CastMediaMessage _message;

        public ChromecastDialogViewModel(CastMediaMessage message)
        {
            _message = message;
            Chromecasts = new ObservableCollection<ChromecastReceiver>();
            CloseCommand = new RelayCommand(() =>
            {
                _message.CastCancellationTokenSource.Cancel();
                OnCloseAction.Invoke();
            });

            RefreshCommand = new RelayCommand(async () =>
            {
                await LoadChromecasts();
            });
        }

        public async Task LoadChromecasts()
        {
            try
            {
                LoadingChromecasts = true;
                IChromecastLocator locator = new MdnsChromecastLocator();
                Chromecasts = new ObservableCollection<ChromecastReceiver>(await locator.FindReceiversAsync());
                LoadingChromecasts = false;
                AnyChromecast = Chromecasts.Any();
                ChooseChromecastDeviceCommand = new RelayCommand<ChromecastReceiver>(device =>
                {
                    ConnectingToChromecast = true;
                    _message.ChromecastReceiver = device;
                    _message.OnCastStarted = () =>
                    {
                        ConnectingToChromecast = false;
                        ConnectedToChromecast = true;
                        CloseCommand.Execute(null);
                    };

                    Task.Run(async () =>
                    {
                        await _message.StartCast.Invoke(device);
                    });
                });
            }
            catch (Exception ex)
            {
                LoadingChromecasts = false;
                AnyChromecast = false;
                Logger.Error(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
                CloseCommand.Execute(null);
            }
        }

        public bool ConnectedToChromecast
        {
            get => _connectedToChromecast;
            set => Set(ref _connectedToChromecast, value);
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => Set(ref _closeCommand, value);
        }

        public ICommand RefreshCommand
        {
            get => _refreshCommand;
            set => Set(ref _refreshCommand, value);
        }

        public ICommand ChooseChromecastDeviceCommand
        {
            get => _chooseChromecastDeviceCommand;
            set => Set(ref _chooseChromecastDeviceCommand, value);
        }

        public ObservableCollection<ChromecastReceiver> Chromecasts
        {
            get => _chromecasts;
            set => Set(ref _chromecasts, value);
        }

        public bool AnyChromecast
        {
            get => _anyChromecast;
            set => Set(ref _anyChromecast, value);
        }

        public bool ConnectingToChromecast
        {
            get => _connectingToChromecast;
            set => Set(ref _connectingToChromecast, value);
        }

        public bool LoadingChromecasts
        {
            get => _loadingChromecasts;
            set => Set(ref _loadingChromecasts, value);
        }
    }
}