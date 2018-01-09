using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GoogleCast;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Services.Chromecast;
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

        private readonly IChromecastService _chromecastService;

        private ObservableCollection<IReceiver> _chromecasts;

        private ICommand _closeCommand;

        private ICommand _chooseChromecastDeviceCommand;

        private ICommand _refreshCommand;

        private ICommand _cancelCommand;

        private bool _anyChromecast;

        private bool _connectedToChromecast;

        public Action OnCloseAction { get; set; }

        private bool _connectingToChromecast;

        private readonly ShowCastMediaMessage _message;

        public ChromecastDialogViewModel(ShowCastMediaMessage message, IChromecastService chromecastService)
        {
            _chromecastService = chromecastService;
            _message = message;
            Chromecasts = new ObservableCollection<IReceiver>();
            CloseCommand = new RelayCommand(() =>
            {
                OnCloseAction.Invoke();
            });

            CancelCommand = new RelayCommand(() =>
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
                Chromecasts = new ObservableCollection<IReceiver>(await _chromecastService.FindReceiversAsync());
                LoadingChromecasts = false;
                AnyChromecast = Chromecasts.Any();
                ChooseChromecastDeviceCommand = new RelayCommand<IReceiver>(async device =>
                {
                    ConnectingToChromecast = true;
                    _message.ChromecastReceiver = device;
                    _message.CloseCastDialog = OnCloseAction;
                    if (await _chromecastService.ConnectAsync(device))
                    {
                        await _message.StartCast.Invoke(device);
                        ConnectingToChromecast = false;
                        ConnectedToChromecast = true;
                    }
                    else
                    {
                        LoadingChromecasts = false;
                        ConnectedToChromecast = false;
                        Messenger.Default.Send(
                            new UnhandledExceptionMessage(
                                new PopcornException($"Could not cast to device {device.FriendlyName}")));
                    }
                });
            }
            catch (Exception ex)
            {
                ConnectedToChromecast = false;
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

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => Set(ref _cancelCommand, value);
        }

        public ICommand ChooseChromecastDeviceCommand
        {
            get => _chooseChromecastDeviceCommand;
            set => Set(ref _chooseChromecastDeviceCommand, value);
        }

        public ObservableCollection<IReceiver> Chromecasts
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