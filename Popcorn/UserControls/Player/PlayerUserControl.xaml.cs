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
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Bandwidth;
using Popcorn.Services.Application;
using Popcorn.Utils;
using Popcorn.Utils.Exceptions;

namespace Popcorn.UserControls.Player
{
    /// <summary>
    /// Interaction logic for PlayerUserControl.xaml
    /// </summary>
    public partial class PlayerUserControl : IDisposable
    {
        /// <summary>
        /// If control is disposed
        /// </summary>
        private bool _disposed;

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
        /// Indicate if user is manipulating the timeline player
        /// </summary>
        private bool UserIsDraggingMediaPlayerSlider { get; set; }

        /// <summary>
        /// Timer used for report time on the timeline
        /// </summary>
        private DispatcherTimer MediaPlayerTimer { get; set; }

        /// <summary>
        /// Subtitle delay
        /// </summary>
        private int SubtitleDelay { get; set; }

        /// <summary>
        /// Report when dragging is used on media player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragStartedEventArgs</param>
        private void MediaSliderProgressDragStarted(object sender, DragStartedEventArgs e)
        {
            UserIsDraggingMediaPlayerSlider = true;
            MediaPlayerIsPlaying = false;
            Player.Pause();
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
            InitializeComponent();
            Loaded += OnLoaded;
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
        /// Get or set the media volume
        /// </summary>
        public int Volume
        {
            get => (int) GetValue(VolumeProperty);

            set => SetValue(VolumeProperty, value);
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose() => Dispose(true);

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
                window.Closing += (s1, e1) => Dispose();
            }

            var vm = DataContext as MediaPlayerViewModel;
            if (vm?.MediaPath == null)
                return;

            vm.SubtitleChosen += OnSubtitleChosen;
            // start the timer used to report time on MediaPlayerSliderProgress
            MediaPlayerTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(200)};
            MediaPlayerTimer.Tick += MediaPlayerTimerTick;
            MediaPlayerTimer.Start();

            // start the activity timer used to manage visibility of the PlayerStatusBar
            ActivityTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            ActivityTimer.Tick += OnInactivity;
            ActivityTimer.Start();

            InputManager.Current.PreProcessInput += OnActivity;

            vm.StoppedMedia += OnStoppedMedia;
            vm.PausedMedia += OnPausedMedia;
            vm.ResumedMedia += OnResumedMedia;
            if (vm.BufferProgress != null)
            {
                vm.BufferProgress.ProgressChanged += OnBufferProgressChanged;
            }

            if (vm.BandwidthRate != null)
            {
                vm.BandwidthRate.ProgressChanged += OnBandwidthChanged;
            }

            Player.VlcMediaPlayer.EndReached += MediaPlayerEndReached;

            if (!string.IsNullOrEmpty(vm.SubtitleFilePath))
            {
                Player.LoadMediaWithOptions(vm.MediaPath, $@":sub-file={vm.SubtitleFilePath}");
            }
            else
            {
                Player.LoadMedia(vm.MediaPath);
            }

            if (vm.MediaType == MediaType.Trailer)
            {
                DownloadProgress.Visibility = Visibility.Collapsed;
            }

            Player.VlcMediaPlayer.EncounteredError += EncounteredError;
            Player.VlcMediaPlayer.Playing += OnPlaying;
            Player.VlcMediaPlayer.Stoped += OnStopped;
            Title.Text = vm.MediaName;
            await Task.Delay(500);
            PlayMedia();
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
            Player.Resume();
        }

        /// <summary>
        /// On player stopped, hide Cast button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStopped(object sender, Meta.Vlc.ObjectEventArgs<Meta.Vlc.Interop.Media.MediaState> e)
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
        private void OnPlaying(object sender, Meta.Vlc.ObjectEventArgs<Meta.Vlc.Interop.Media.MediaState> e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CastButton.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// On pause player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPausedMedia(object sender, EventArgs e)
        {
            Player.Pause();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
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
        private static void OnVolumeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var moviePlayer = obj as PlayerUserControl;
            if (moviePlayer == null)
                return;

            var newVolume = (int) e.NewValue;
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
            MediaPlayerIsPlaying = true;

            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            Player.Play();
        }

        /// <summary>
        /// Pause the movie
        /// </summary>
        private void PauseMedia()
        {
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
        private void OnStoppedMedia(object sender, EventArgs e) => Dispose();

        /// <summary>
        /// Report the playing progress on the timeline
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerTimerTick(object sender, EventArgs e)
        {
            if ((Player == null) || (UserIsDraggingMediaPlayerSlider)) return;
            MediaPlayerSliderProgress.Minimum = 0;
            MediaPlayerSliderProgress.Maximum = Player.Length.TotalMilliseconds;
            MediaPlayerSliderProgress.Value = Player.Time.TotalMilliseconds;
        }

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
            UserIsDraggingMediaPlayerSlider = false;
            Player.Time = TimeSpan.FromMilliseconds(MediaPlayerSliderProgress.Value);
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
                DownloadProgress.BeginAnimation(OpacityProperty, opacityAnimation);
                TitlePanel.BeginAnimation(OpacityProperty, opacityAnimation);
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
            DownloadProgress.BeginAnimation(OpacityProperty, opacityAnimation);
            TitlePanel.BeginAnimation(OpacityProperty, opacityAnimation);
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Cursor = Cursors.Arrow;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            MouseActivitySemaphore.Release();
        }

        /// <summary>
        /// Dispose the control
        /// </summary>
        /// <param name="disposing">If a disposing is already processing</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Loaded -= OnLoaded;

            MediaPlayerTimer.Tick -= MediaPlayerTimerTick;
            MediaPlayerTimer.Stop();

            ActivityTimer.Tick -= OnInactivity;
            ActivityTimer.Stop();

            InputManager.Current.PreProcessInput -= OnActivity;

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

            Task.Run(async () =>
            {
                await Task.Delay(500);
                Player.Dispose();
            });

            _disposed = true;
            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}