using System;
using System.Collections.Generic;
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
using System.Linq;
using System.Windows.Controls;
using GalaSoft.MvvmLight.CommandWpf;
using Popcorn.Converters;
using Popcorn.FFME;
using Popcorn.Models.Chromecast;
using Popcorn.Models.Download;
using MediaElement = Popcorn.FFME.MediaElement;
using MouseButton = System.Windows.Input.MouseButton;

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

        private ICommand _setLowerSubtitleSizeCommand;

        private ICommand _setHigherSubtitleSizeCommand;

        /// <summary>
        /// Application service
        /// </summary>
        private readonly IApplicationService _applicationService;

        /// <summary>
        /// Initializes a new instance of the MoviePlayer class.
        /// </summary>
        public PlayerUserControl()
        {
            MediaElement.FFmpegDirectory = Constants.FFmpegPath;
            _applicationService = SimpleIoc.Default.GetInstance<IApplicationService>();
            InitializeComponent();
            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDownEvent));
            PositionSlider.AddHandler
            (
                Slider.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnSliderMouseLeftButtonDown),
                true
            );
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Toggle playing media when space key is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewKeyDownEvent(object sender,
            RoutedEventArgs e)
        {
            KeyEventArgs ke = e as KeyEventArgs;
            ke.Handled = true;
            if (ke.Key == Key.Space)
            {
                if (Media.IsPlaying)
                    PauseMedia();
                else
                    PlayMedia();
            }

            if (ke.Key == Key.Up)
            {
                Media.Volume += 0.05;
            }

            if (ke.Key == Key.Down)
            {
                Media.Volume -= 0.05;
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

        private PieceAvailability _pieceAvailability;

        private bool _isPausedForBuffering;

        /// <summary>
        /// Subscribe to events and play the movie when control has been loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnLoaded(object sender, EventArgs e)
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

            var applicationSettings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
            Subtitles.FontSize = applicationSettings.SelectedSubtitleSize?.Size ?? 22;
            Subtitles.Foreground = new SolidColorBrush(applicationSettings.SubtitlesColor);
            // start the activity timer used to manage visibility of the PlayerStatusBar
            ActivityTimer = new DispatcherTimer(DispatcherPriority.Background) {Interval = TimeSpan.FromSeconds(2)};
            ActivityTimer.Tick += OnInactivity;
            ActivityTimer.Start();

            InputManager.Current.PreProcessInput += OnActivity;

            SetLowerSubtitleSizeCommand = new RelayCommand(() =>
            {
                Subtitles.FontSize--;
            });

            SetHigherSubtitleSizeCommand = new RelayCommand(() =>
            {
                Subtitles.FontSize++;
            });

            vm.StoppedMedia += OnStoppedMedia;
            vm.PausedMedia += OnPausedMedia;
            vm.ResumedMedia += OnResumedMedia;
            vm.CastStarted += OnCastStarted;
            vm.CastStopped += OnCastStopped;
            vm.CastStatusChanged += OnCastStatusChanged;
            if (vm.PieceAvailability != null)
            {
                vm.PieceAvailability.ProgressChanged += PieceAvailabilityOnProgressChanged;
            }

            if (vm.BandwidthRate != null)
            {
                vm.BandwidthRate.ProgressChanged += OnBandwidthChanged;
            }

            Media.RenderingVideo += OnRenderingVideo;
            Media.MediaEnded += MediaPlayerEndReached;
            Media.Source = new Uri(vm.MediaPath);
            if (vm.MediaType == MediaType.Trailer)
            {
                DownloadProgress.Visibility = Visibility.Collapsed;
            }

            Media.MediaFailed += EncounteredError;
            Media.VolumeChanged += OnVolumeChanged;
            Title.Text = vm.MediaName;
            PlayMedia();
        }

        private void PieceAvailabilityOnProgressChanged(object sender, PieceAvailability pieceAvailability)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (!Media.NaturalDuration.HasTimeSpan)
                    return;

                if (!(DataContext is MediaPlayerViewModel vm))
                    return;

                vm.MediaLength = Media.NaturalDuration.TimeSpan.TotalSeconds;
                vm.PlayerTime = PositionSlider.Value;
                _pieceAvailability = pieceAvailability;
                double startPieceAvailabilityPercentage =
                    (double) _pieceAvailability.StartAvailablePiece / (double) _pieceAvailability.TotalPieces;
                double endPieceAvailabilityPercentage =
                    (double) _pieceAvailability.EndAvailablePiece / (double) _pieceAvailability.TotalPieces;
                var playPercentage = PositionSlider.Value / Media.NaturalDuration.TimeSpan.TotalSeconds;

                if (_isPausedForBuffering && playPercentage > startPieceAvailabilityPercentage &&
                    playPercentage < endPieceAvailabilityPercentage)
                {
                    _isPausedForBuffering = false;
                    Buffering.Visibility = Visibility.Collapsed;
                    Media.Position = TimeSpan.FromSeconds(PositionSlider.Value);
                    PlayMedia();
                }
                else if (!_isPausedForBuffering)
                {
                    PositionSlider.Value = Media.Position.TotalSeconds;
                }
            });
        }

        private void OnRenderingVideo(object sender, RenderingVideoEventArgs e)
        {
            if (!(DataContext is MediaPlayerViewModel vm))
                return;

            if (vm.MediaType == MediaType.Trailer)
            {
                PositionSlider.Value = Media.Position.TotalSeconds;
            }

            if (vm.SubtitleItems.Any())
            {
                var subtitle = vm.SubtitleItems.FirstOrDefault(a =>
                    a.StartTime <= Media.Position.TotalMilliseconds + SubtitleDelay &&
                    a.EndTime > Media.Position.TotalMilliseconds + SubtitleDelay);
                if (subtitle == null)
                {
                    Subtitles.Text = string.Empty;
                    return;
                }

                var lines = subtitle.Lines;
                var formattedLines = new List<string>();
                foreach (var line in lines)
                {
                    formattedLines.Add(line.Replace("<b>", "").Replace("</b>", "")
                        .Replace("<i>", "").Replace("</i>", "").Replace("<u>", "")
                        .Replace("</u>", ""));
                }

                Subtitles.Text = string.Join(Environment.NewLine, formattedLines);
            }
        }

        /// <summary>
        /// When cast is being played
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCastStatusChanged(object sender, MediaStatusEventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (e.Status?.PlayerState == "PLAYING")
                {
                    Media.Position = TimeSpan.FromSeconds(e.Status.CurrentTime);
                    if (!Media.IsPlaying)
                        PlayMedia();
                }
            });
        }

        /// <summary>
        /// Unmute media when cast has stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCastStopped(object sender, EventArgs e)
        {
            Media.IsMuted = false;
        }

        /// <summary>
        /// Mute media when cast has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCastStarted(object sender, EventArgs e)
        {
            Media.IsMuted = true;
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
        /// On pause player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPausedMedia(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(PauseMedia);
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
                _applicationService.IsFullScreen = !_applicationService.IsFullScreen;
            }

            if (e.Key == Key.Space)
            {
                if (Media.IsPlaying)
                    PauseMedia();
                else
                    PlayMedia();
            }

            if (!(DataContext is MediaPlayerViewModel vm) || !vm.SubtitleItems.Any())
                return;
            switch (e.Key)
            {
                case Key.H:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        SubtitleDelay += 1000;
                    }
                    else
                    {
                        SubtitleDelay += 1000;
                    }
                    break;
                case Key.G:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        SubtitleDelay -= 1000;
                    }
                    else
                    {
                        SubtitleDelay -= 1000;
                    }
                    break;
                default:
                    return;
            }

            Delay.Text = $"Subtitle delay: {SubtitleDelay} ms";
            if (SubtitleDelaySemaphore.CurrentCount == 0) return;
            await SubtitleDelaySemaphore.WaitAsync();
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
                Remaining.Text = new TimeSpanFormatter().Convert(e.ETA, null, null, null).ToString();
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
        /// <param name="sender">Sender</param>
        /// <param name="args">Args</param>
        private async void OnVolumeChanged(object sender, VolumeEventArgs args)
        {
            var newVolume = args.Volume;
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting)
            {
                await vm.SetVolume(Convert.ToSingle(newVolume));
            }
        }

        /// <summary>
        /// When user uses the mousewheel, update the volume
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">MouseWheelEventArgs</param>
        private void MouseWheelMediaPlayer(object sender, MouseWheelEventArgs e)
        {
            Media.Volume += e.Delta > 0 ? 0.05 : -0.05;
        }

        /// <summary>
        /// When a movie has been seen, save this information in the user data
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerEndReached(object sender, EventArgs e)
            => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
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
            if (_isPausedForBuffering)
                return;

            try
            {
                _applicationService.SwitchConstantDisplayAndPower(true);
                Media.Play();
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
                CastButton.Visibility = Visibility.Visible;
                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null && Media.NaturalDuration.HasTimeSpan)
                {
                    vm.MediaDuration = Media.NaturalDuration.TimeSpan.TotalSeconds;
                    if (vm.IsCasting)
                        vm.PlayCastCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException("An error has occured while trying to play the media.")));
                var vm = DataContext as MediaPlayerViewModel;
                if (vm == null)
                    return;

                vm.MediaEnded();
            }
        }

        /// <summary>
        /// Pause the movie
        /// </summary>
        private void PauseMedia()
        {
            try
            {
                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null && vm.IsCasting)
                {
                    vm.PauseCastCommand.Execute(null);
                }

                _applicationService.SwitchConstantDisplayAndPower(false);
                Media.Pause();
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
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
            e.CanExecute = !Media.IsPlaying && !_isPausedForBuffering;
            if (Media.IsPlaying)
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
            e.CanExecute = Media.CanPause;
            if (Media.IsPlaying)
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

                Media.MediaFailed -= EncounteredError;
                Media.MediaEnded -= MediaPlayerEndReached;
                var window = System.Windows.Window.GetWindow(this);
                if (window != null)
                {
                    window.KeyDown -= OnKeyDown;
                    window.Cursor = Cursors.Arrow;
                }

                var vm = DataContext as MediaPlayerViewModel;
                if (vm != null)
                {
                    vm.StoppedMedia -= OnStoppedMedia;
                    vm.ResumedMedia -= OnResumedMedia;
                    vm.PausedMedia -= OnPausedMedia;
                }

                if (vm?.BandwidthRate != null)
                {
                    vm.BandwidthRate.ProgressChanged -= OnBandwidthChanged;
                }

                Media.Dispose();
                _applicationService.SwitchConstantDisplayAndPower(false);
                RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDownEvent));
                PositionSlider.RemoveHandler(Slider.PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(OnSliderMouseLeftButtonDown));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void OnMediaSliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            SeekMedia();
        }

        private void SeekMedia()
        {
            if (_isPausedForBuffering) return;
            Media.Position = TimeSpan.FromSeconds(PositionSlider.Value);
            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null && vm.IsCasting)
            {
                vm.SeekCastCommand.Execute(Media.Position.TotalSeconds);
            }

            PlayMedia();
        }

        private void OnMediaSliderDragStarted(object sender, DragStartedEventArgs e)
        {
            PauseMedia();
        }

        private void OnSliderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SeekMedia();
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Media.IsPlaying)
                PauseMedia();
            else
                PlayMedia();
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_pieceAvailability == null || !Media.NaturalDuration.HasTimeSpan)
                return;

            double startPieceAvailabilityPercentage =
                (double) _pieceAvailability.StartAvailablePiece / (double) _pieceAvailability.TotalPieces;
            double endPieceAvailabilityPercentage =
                (double) _pieceAvailability.EndAvailablePiece / (double) _pieceAvailability.TotalPieces;
            var playPercentage = e.NewValue / Media.NaturalDuration.TimeSpan.TotalSeconds;
            if (playPercentage < startPieceAvailabilityPercentage ||
                playPercentage > endPieceAvailabilityPercentage)
            {
                Buffering.Visibility = Visibility.Visible;
                _isPausedForBuffering = true;
                PauseMedia();
            }
        }
    }
}