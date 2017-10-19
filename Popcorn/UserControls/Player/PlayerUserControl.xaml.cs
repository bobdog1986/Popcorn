using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using Popcorn.ViewModels.Pages.Player;
using System.Threading;
using System.Windows.Media;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Services.Application;
using Popcorn.Utils;
using Popcorn.Utils.Exceptions;
using Popcorn.ViewModels.Windows.Settings;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using GalaSoft.MvvmLight.CommandWpf;

namespace Popcorn.UserControls.Player
{
    /// <summary>
    /// Interaction logic for PlayerUserControl.xaml
    /// </summary>
    public partial class PlayerUserControl : INotifyPropertyChanged
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Indicates if a media is playing
        /// </summary>
        private bool MediaPlayerIsPlaying { get; set; }

        /// <summary>
        /// Used to update the activity mouse and mouse position.
        /// </summary>
        private DispatcherTimer ActivityTimer { get; set; }

        /// <summary>
        /// Get or set the mouse position when inactive
        /// </summary>
        private Point InactiveMousePosition { get; set; } = new Point(0, 0);

        /// <summary>
        /// Subtitle delay
        /// </summary>
        private int SubtitleDelay { get; set; }

        /// <summary>
        /// Cast player time in seconds
        /// </summary>
        private double CastPlayerTimeInSeconds { get; set; }

        private ICommand _setLowerSubtitleSizeCommand;

        private ICommand _setHigherSubtitleSizeCommand;

        /// <summary>
        /// Application service
        /// </summary>
        private readonly IApplicationService _applicationService;

        /// <summary>
        /// Subtitle size
        /// </summary>
        private int _subtitleSize;

        /// <summary>
        /// Report when dragging is used on media player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragStartedEventArgs</param>
        private void MediaSliderProgressDragStarted(object sender, DragStartedEventArgs e)
        {
            MediaPlayerIsPlaying = false;
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting)
            {
                vm.PauseCastCommand.Execute(null);
            }
            else
            {
                Player.Pause();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Volume" /> dependency property.
        /// </summary>
        internal static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume",
            typeof(int),
            typeof(PlayerUserControl), new PropertyMetadata(100, OnVolumeChanged));

        /// <summary>
        /// Initializes a new instance of the MoviePlayer class.
        /// </summary>
        public PlayerUserControl()
        {
            int HexConverter(Color c)
            {
                var hex = c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
                return Convert.ToInt32(hex, 16);
            }

            var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
            _applicationService = SimpleIoc.Default.GetInstance<IApplicationService>();
            _subtitleSize = applicationSettings.SelectedSubtitleSize.Size;
            VlcOptions = new[]
            {
                "-I", "--dummy-quiet", "--no-video-title", "--no-sub-autodetect-file",
                $"--freetype-color={HexConverter(applicationSettings.SubtitlesColor)}",
                $"--freetype-rel-fontsize={applicationSettings.SelectedSubtitleSize.Size}",
                "--file-caching=5000", "--network-caching=5000"
            };
            InitializeComponent();
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(OnPreviewKeyDownEvent));

            Loaded += OnLoaded;
        }

        private void OnPreviewKeyDownEvent(object sender,
            RoutedEventArgs e)
        {
            KeyEventArgs ke = e as KeyEventArgs;
            if (ke.Key == Key.Space)
            {
                ke.Handled = true;
                if (MediaPlayerIsPlaying)
                    PauseMedia();
                else
                    PlayMedia();
            }
        }

        /// <summary>
        /// Semaphore used to update mouse activity
        /// </summary>
        private static readonly SemaphoreSlim MouseActivitySemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Semaphore used to update subtitle delay
        /// </summary>
        private static readonly SemaphoreSlim SubtitleDelaySemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Represents the player current time in milliseconds
        /// </summary>
        public double TimeInMilliseconds
        {
            get
            {
                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null)
                {
                    vm.PlayerTime = Player.Time.TotalMilliseconds / 1000d;
                    if (vm.IsCasting)
                    {
                        return CastPlayerTimeInSeconds * 1000d;
                    }

                    return Player.Time.TotalMilliseconds;
                }

                return Player.Time.TotalMilliseconds;
            }
            set
            {
                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null && vm.IsCasting)
                {
                    CastPlayerTimeInSeconds = value / 1000d;
                }

                Player.Time = TimeSpan.FromMilliseconds(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get or set the media volume
        /// </summary>
        public int Volume
        {
            get => (int) GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public string[] VlcOptions { get; set; }

        /// <summary>
        /// Subscribe to events and play the movie when control has been loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private async void OnLoaded(object sender, EventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.KeyDown += OnKeyDown;
                window.Closing += (s1, e1) => Unload();
            }

            var vm = DataContext as MediaPlayerViewModel;
            if (vm?.MediaPath == null)
                return;

            vm.SubtitleChosen += OnSubtitleChosen;
            Player.TimeChanged += OnTimeChanged;

            // start the activity timer used to manage visibility of the PlayerStatusBar
            ActivityTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            ActivityTimer.Tick += OnInactivity;
            ActivityTimer.Start();

            InputManager.Current.PreProcessInput += OnActivity;

            SetLowerSubtitleSizeCommand = new RelayCommand(() =>
            {
                if (_subtitleSize == 12)
                {
                    Player.VlcOption[5] = $"--freetype-rel-fontsize={_subtitleSize}";
                }
                else
                {
                    Player.VlcOption[5] = $"--freetype-rel-fontsize={_subtitleSize - 2}";
                    _subtitleSize -= 2;
                }
            });

            SetHigherSubtitleSizeCommand = new RelayCommand(() =>
            {
                if (_subtitleSize == 20)
                {
                    Player.VlcOption[5] = $"--freetype-rel-fontsize={_subtitleSize}";
                }
                else
                {
                    Player.VlcOption[5] = $"--freetype-rel-fontsize={_subtitleSize + 2}";
                    _subtitleSize += 2;
                }
            });

            vm.StoppedMedia += OnStoppedMedia;
            vm.PausedMedia += OnPausedMedia;
            vm.ResumedMedia += OnResumedMedia;
            vm.CastStarted += OnCastStarted;
            vm.CastStopped += OnCastStopped;
            if (vm.BufferProgress != null)
            {
                vm.BufferProgress.ProgressChanged += OnBufferProgressChanged;
            }

            if (vm.BandwidthRate != null)
            {
                vm.BandwidthRate.ProgressChanged += OnBandwidthChanged;
            }

            Player.VlcMediaPlayer.EndReached += MediaPlayerEndReached;
            Player.LoadMedia(vm.MediaPath);
            if (vm.MediaType == MediaType.Trailer)
            {
                DownloadProgress.Visibility = Visibility.Collapsed;
            }

            Player.VlcMediaPlayer.EncounteredError += EncounteredError;
            Player.VlcMediaPlayer.Playing += OnPlaying;
            Player.VlcMediaPlayer.Stoped += OnStopped;
            vm.CastPlayerTimeChanged += CastPlayerTimeChanged;
            Title.Text = vm.MediaName;
            await Task.Delay(500);
            PlayMedia();
            if (!string.IsNullOrEmpty(vm.SubtitleFilePath))
            {
                Player.VlcMediaPlayer.SetSubtitleFile(vm.SubtitleFilePath);
            }
        }

        private void OnCastStopped(object sender, EventArgs e)
        {
            if (Player.VlcMediaPlayer.IsMute)
                Player.VlcMediaPlayer.ToggleMute();
        }

        private void OnCastStarted(object sender, EventArgs e)
        {
            if (!Player.VlcMediaPlayer.IsMute)
                Player.VlcMediaPlayer.ToggleMute();
        }

        /// <summary>
        /// On cast time player changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CastPlayerTimeChanged(object sender, Events.TimeChangedEventArgs e)
        {
            if (MediaPlayerIsPlaying)
            {
                CastPlayerTimeInSeconds = e.Seconds;
                MediaPlayerSliderProgress.Minimum = 0;
                MediaPlayerSliderProgress.Maximum = Player.Length.TotalMilliseconds;
                OnPropertyChanged(nameof(TimeInMilliseconds));
            }
        }

        /// <summary>
        /// Report the playing progress on the timeline
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnTimeChanged(object sender, EventArgs e)
        {
            MediaPlayerSliderProgress.Minimum = 0;
            MediaPlayerSliderProgress.Maximum = Player.Length.TotalMilliseconds;
            OnPropertyChanged(nameof(TimeInMilliseconds));
        }

        /// <summary>
        /// On Subtitle Chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSubtitleChosen(object sender, Events.SubtitleChangedEventArgs e)
        {
            Player.VlcMediaPlayer.SetSubtitleFile(e.SubtitlePath);
        }

        /// <summary>
        /// On player resumed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResumedMedia(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(PlayMedia);
        }

        /// <summary>
        /// On player stopped, hide Cast button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStopped(object sender, Popcorn.Vlc.ObjectEventArgs<Popcorn.Vlc.Interop.Media.MediaState> e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CastButton.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// On player started, show Cast button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaying(object sender, Popcorn.Vlc.ObjectEventArgs<Popcorn.Vlc.Interop.Media.MediaState> e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CastButton.Visibility = Visibility.Visible;
                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null)
                {
                    vm.MediaDuration = Player.Length.TotalSeconds;
                }
            });
        }

        /// <summary>
        /// On pause player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPausedMedia(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(PauseMedia);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MediaPlayerIsPlaying)
                PauseMedia();
            else
                PlayMedia();
        }

        /// <summary>
        /// On key down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                var applicationService = SimpleIoc.Default.GetInstance<IApplicationService>();
                applicationService.IsFullScreen = !applicationService.IsFullScreen;
            }

            if (e.Key == Key.Space)
            {
                if (MediaPlayerIsPlaying)
                    PauseMedia();
                else
                    PlayMedia();
            }

            if (Player.VlcMediaPlayer.SubtitleCount == 0) return;
            switch (e.Key)
            {
                case Key.H:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        SubtitleDelay += 1000000;
                    }
                    else
                    {
                        SubtitleDelay += 100000;
                    }
                    break;
                case Key.G:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        SubtitleDelay -= 1000000;
                    }
                    else
                    {
                        SubtitleDelay -= 100000;
                    }
                    break;
                default:
                    return;
            }

            Delay.Text = $"Subtitle delay: {SubtitleDelay / 1000} ms";
            if (SubtitleDelaySemaphore.CurrentCount == 0) return;
            await SubtitleDelaySemaphore.WaitAsync();
            Player.VlcMediaPlayer.SubtitleDelay = SubtitleDelay;
            var increasedPanelOpacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(0.6, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };
            var increasedSubtitleDelayOpacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            SubtitleDelayPanel.BeginAnimation(OpacityProperty, increasedPanelOpacityAnimation);
            Delay.BeginAnimation(OpacityProperty, increasedSubtitleDelayOpacityAnimation);
            await Task.Delay(2000);
            var decreasedOpacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            SubtitleDelayPanel.BeginAnimation(OpacityProperty, decreasedOpacityAnimation);
            Delay.BeginAnimation(OpacityProperty, decreasedOpacityAnimation);
            SubtitleDelaySemaphore.Release();
        }

        /// <summary>
        /// When bandwidth rate has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBandwidthChanged(object sender, BandwidthRate e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Download.Text = e.DownloadRate.ToString(CultureInfo.InvariantCulture);
                Upload.Text = e.UploadRate.ToString(CultureInfo.InvariantCulture);
            });
        }

        /// <summary>
        /// When buffer progress has changed, update buffer bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBufferProgressChanged(object sender, double e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                BufferProgress.Value = Math.Round(e);
            });
        }

        /// <summary>
        /// Vlc encounters an error. Warn the user of this
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EncounteredError(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException("An error has occured while trying to play the media.")));
                var vm = DataContext as MediaPlayerViewModel;
                if (vm == null)
                    return;

                vm.MediaEnded();
            });
        }

        /// <summary>
        /// When media's volume changed, update volume
        /// </summary>
        /// <param name="e">e</param>
        /// <param name="obj">obj</param>
        private static async void OnVolumeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var moviePlayer = obj as PlayerUserControl;
            if (moviePlayer == null)
                return;

            var newVolume = (int) e.NewValue;
            var vm = moviePlayer.DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting)
            {
                await vm.SetVolume(newVolume / 200d);
            }

            moviePlayer.ChangeMediaVolume(newVolume);
        }

        /// <summary>
        /// Change the media's volume
        /// </summary>
        /// <param name="newValue">New volume value</param>
        private void ChangeMediaVolume(int newValue) => Player.Volume = newValue;

        /// <summary>
        /// When user uses the mousewheel, update the volume
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">MouseWheelEventArgs</param>
        private void MouseWheelMediaPlayer(object sender, MouseWheelEventArgs e)
        {
            if ((Volume <= 190 && e.Delta > 0) || (Volume >= 10 && e.Delta < 0))
                Volume += (e.Delta > 0) ? 10 : -10;
        }

        /// <summary>
        /// When a movie has been seen, save this information in the user data
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerEndReached(object sender, EventArgs e)
            => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (Player.Position < 0.99) return;

                var vm = DataContext as MediaPlayerViewModel;
                if (vm == null)
                    return;

                vm.MediaEnded();
            });

        /// <summary>
        /// Play the movie
        /// </summary>
        private void PlayMedia()
        {
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting && vm.IsCastPaused)
            {
                vm.PlayCastCommand.Execute(null);
            }

            _applicationService.SwitchConstantDisplayAndPower(true);
            Player.Play();
            MediaPlayerIsPlaying = true;
            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Pause the movie
        /// </summary>
        private void PauseMedia()
        {
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting && vm.IsCastPlaying)
            {
                vm.PauseCastCommand.Execute(null);
            }

            _applicationService.SwitchConstantDisplayAndPower(false);
            Player.Pause();
            MediaPlayerIsPlaying = false;
            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// When media has finished playing, dispose the control
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnStoppedMedia(object sender, EventArgs e) => Unload();

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = Player != null;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the media player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = MediaPlayerIsPlaying;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Report when user has finished dragging the media player progress
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragCompletedEventArgs</param>
        private void MediaSliderProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting)
            {
                vm.SeekCastCommand.Execute(CastPlayerTimeInSeconds);
                vm.PlayCastCommand.Execute(null);
            }

            Player.Resume();
            MediaPlayerIsPlaying = true;
        }

        /// <summary>
        /// Report runtime when trailer player progress changed
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">RoutedPropertyChangedEventArgs</param>
        private void MediaSliderProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MoviePlayerTextProgressStatus.Text =
                TimeSpan.FromMilliseconds(MediaPlayerSliderProgress.Value)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture) + " / " +
                TimeSpan.FromMilliseconds(Player.Length.TotalMilliseconds)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Play media
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">ExecutedRoutedEventArgs</param>
        private void MediaPlayerPlayExecuted(object sender, ExecutedRoutedEventArgs e) => PlayMedia();

        /// <summary>
        /// Pause media
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseExecuted(object sender, ExecutedRoutedEventArgs e) => PauseMedia();

        /// <summary>
        /// Hide the PlayerStatusBar on mouse inactivity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnInactivity(object sender, EventArgs e)
        {
            if (InactiveMousePosition == Mouse.GetPosition(Container))
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window != null)
                {
                    window.Cursor = Cursors.None;
                }

                var opacityAnimation = new DoubleAnimationUsingKeyFrames
                {
                    Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                    KeyFrames = new DoubleKeyFrameCollection
                    {
                        new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1d), new PowerEase
                        {
                            EasingMode = EasingMode.EaseInOut
                        })
                    }
                };

                PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);
                UpperPanel.BeginAnimation(OpacityProperty, opacityAnimation);
            }

            InactiveMousePosition = Mouse.GetPosition(Container);
        }

        /// <summary>
        /// Show the PlayerStatusBar on mouse activity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private async void OnActivity(object sender, PreProcessInputEventArgs e)
        {
            await MouseActivitySemaphore.WaitAsync();
            if (e.StagingItem == null)
            {
                MouseActivitySemaphore.Release();
                return;
            }

            var inputEventArgs = e.StagingItem.Input;
            if (!(inputEventArgs is MouseEventArgs) && !(inputEventArgs is KeyboardEventArgs))
            {
                MouseActivitySemaphore.Release();
                return;
            }
            var mouseEventArgs = e.StagingItem.Input as MouseEventArgs;

            // no button is pressed and the position is still the same as the application became inactive
            if (mouseEventArgs?.LeftButton == MouseButtonState.Released &&
                mouseEventArgs.RightButton == MouseButtonState.Released &&
                mouseEventArgs.MiddleButton == MouseButtonState.Released &&
                mouseEventArgs.XButton1 == MouseButtonState.Released &&
                mouseEventArgs.XButton2 == MouseButtonState.Released &&
                InactiveMousePosition == mouseEventArgs.GetPosition(Container))
            {
                MouseActivitySemaphore.Release();
                return;
            }

            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);
            UpperPanel.BeginAnimation(OpacityProperty, opacityAnimation);
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Cursor = Cursors.Arrow;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            MouseActivitySemaphore.Release();
        }

        /// <summary>
        /// Command used to lower subtitle size
        /// </summary>
        public ICommand SetLowerSubtitleSizeCommand
        {
            get => _setLowerSubtitleSizeCommand;
            set
            {
                _setLowerSubtitleSizeCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command used to lower subtitle size
        /// </summary>
        public ICommand SetHigherSubtitleSizeCommand
        {
            get => _setHigherSubtitleSizeCommand;
            set
            {
                _setHigherSubtitleSizeCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Dispose the control
        /// </summary>
        private void Unload()
        {
            try
            {
                Loaded -= OnLoaded;
                ActivityTimer.Tick -= OnInactivity;
                ActivityTimer.Stop();

                InputManager.Current.PreProcessInput -= OnActivity;

                Player.TimeChanged -= OnTimeChanged;
                Player.VlcMediaPlayer.EncounteredError -= EncounteredError;
                Player.VlcMediaPlayer.EndReached -= MediaPlayerEndReached;
                MediaPlayerIsPlaying = false;
                var window = System.Windows.Window.GetWindow(this);
                if (window != null)
                {
                    window.KeyDown -= OnKeyDown;
                    window.Cursor = Cursors.Arrow;
                }

                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null)
                {
                    vm.CastPlayerTimeChanged -= CastPlayerTimeChanged;
                    vm.SubtitleChosen -= OnSubtitleChosen;
                    vm.StoppedMedia -= OnStoppedMedia;
                    vm.ResumedMedia -= OnResumedMedia;
                    vm.PausedMedia -= OnPausedMedia;
                }

                if (vm?.BufferProgress != null)
                {
                    vm.BufferProgress.ProgressChanged -= OnBufferProgressChanged;
                }

                if (vm?.BandwidthRate != null)
                {
                    vm.BandwidthRate.ProgressChanged -= OnBandwidthChanged;
                }

                Player.Dispose();
                _applicationService.SwitchConstantDisplayAndPower(false);
                RemoveHandler(Keyboard.PreviewKeyDownEvent,
                    new KeyEventHandler(OnPreviewKeyDownEvent));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}