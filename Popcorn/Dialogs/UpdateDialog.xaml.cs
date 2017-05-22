using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using Popcorn.Helpers;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Manage update settings
    /// </summary>
    public class UpdateDialogSettings : MetroDialogSettings
    {
        /// <summary>
        /// Initialize a new instance of UpdateDialogSettings
        /// </summary>
        /// <param name="title">The dialog title</param>
        /// <param name="message">The dialog message</param>
        /// <param name="releaseNotes">The releases notes to display</param>
        public UpdateDialogSettings(string title, string message, string releaseNotes)
        {
            Title = title;
            Message = message;
            ReleaseNotes = releaseNotes;
        }

        /// <summary>
        /// Dialog title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Dialog message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Releases notes to display
        /// </summary>
        public string ReleaseNotes { get; }
    }

    /// <summary>
    /// Data to return on dialog closing
    /// </summary>
    public class UpdateDialogData
    {
        /// <summary>
        /// Specify if application should be restar
        /// </summary>
        public bool Restart { get; set; }
    }

    /// <summary>
    /// Manage update dialkog
    /// </summary>
    public partial class UpdateDialog
    {
        /// <summary>
        /// Message property
        /// </summary>
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message",
            typeof (string), typeof (UpdateDialog), new PropertyMetadata(default(string)));

        /// <summary>
        /// Releases notes property
        /// </summary>
        public static readonly DependencyProperty ReleaseNotesProperty = DependencyProperty.Register("ReleaseNotes",
            typeof (string), typeof (UpdateDialog), new PropertyMetadata(default(string)));

        /// <summary>
        /// Restart button text property
        /// </summary>
        public static readonly DependencyProperty RestartButtonTextProperty =
            DependencyProperty.Register("RestartButtonText", typeof (string), typeof (UpdateDialog),
                new PropertyMetadata(LocalizationProviderHelper.GetLocalizedValue<string>("NowLabel")));

        /// <summary>
        /// Later button text property
        /// </summary>
        public static readonly DependencyProperty LaterButtonTextProperty =
            DependencyProperty.Register("LaterButtonText", typeof (string), typeof (UpdateDialog),
                new PropertyMetadata(LocalizationProviderHelper.GetLocalizedValue<string>("LaterLabel")));

        /// <summary>
        /// Initialize a new instance of UpdateDialog
        /// </summary>
        /// <param name="settings">The dialog settings</param>
        internal UpdateDialog(UpdateDialogSettings settings)
        {
            InitializeComponent();
            Message = settings.Message;
            Title = settings.Title;
            ReleaseNotes = settings.ReleaseNotes;
        }

        /// <summary>
        /// Dialog message
        /// </summary>
        public string Message
        {
            get { return (string) GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        /// <summary>
        /// Releases notes to display
        /// </summary>
        public string ReleaseNotes
        {
            get { return (string) GetValue(ReleaseNotesProperty); }
            set { SetValue(ReleaseNotesProperty, value); }
        }

        /// <summary>
        /// Restart button text
        /// </summary>
        public string RestartButtonText
        {
            get { return (string) GetValue(RestartButtonTextProperty); }
            set { SetValue(RestartButtonTextProperty, value); }
        }

        /// <summary>
        /// Later buttont text
        /// </summary>
        public string LaterButtonText
        {
            get { return (string) GetValue(LaterButtonTextProperty); }
            set { SetValue(LaterButtonTextProperty, value); }
        }

        /// <summary>
        /// Asynchronous task, waiting for button press event to complete
        /// </summary>
        /// <returns></returns>
        internal Task<UpdateDialogData> WaitForButtonPressAsync()
        {
            var tcs = new TaskCompletionSource<UpdateDialogData>();

            RoutedEventHandler restartHandler = null;
            KeyEventHandler restartKeyHandler = null;

            RoutedEventHandler laterHandler = null;
            KeyEventHandler laterKeyHandler = null;

            KeyEventHandler escapeKeyHandler = null;

            Action cleanUpHandlers = null;

            var cancellationTokenRegistration = DialogSettings.CancellationToken.Register(() =>
            {
                cleanUpHandlers();
                tcs.TrySetResult(null);
            });

            cleanUpHandlers = () =>
            {
                KeyDown -= escapeKeyHandler;

                PART_RestartButton.Click -= restartHandler;

                PART_RestartButton.KeyDown -= restartKeyHandler;

                PART_LaterButton.Click -= laterHandler;

                PART_LaterButton.KeyDown -= laterKeyHandler;

                cancellationTokenRegistration.Dispose();
            };

            escapeKeyHandler = (sender, e) =>
            {
                if (e.Key != Key.Escape) return;
                cleanUpHandlers();

                tcs.TrySetResult(null);
            };

            restartKeyHandler = (sender, e) =>
            {
                if (e.Key != Key.Enter) return;
                cleanUpHandlers();

                tcs.TrySetResult(new UpdateDialogData
                {
                    Restart = true
                });
            };

            restartHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(new UpdateDialogData
                {
                    Restart = true
                });

                e.Handled = true;
            };

            laterKeyHandler = (sender, e) =>
            {
                if (e.Key != Key.Enter) return;
                cleanUpHandlers();

                tcs.TrySetResult(new UpdateDialogData
                {
                    Restart = false
                });
            };

            laterHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(new UpdateDialogData
                {
                    Restart = false
                });

                e.Handled = true;
            };

            PART_RestartButton.KeyDown += restartKeyHandler;

            PART_LaterButton.KeyDown += laterKeyHandler;

            KeyDown += escapeKeyHandler;

            PART_RestartButton.Click += restartHandler;

            PART_LaterButton.Click += laterHandler;

            return tcs.Task;
        }

        /// <summary>
        /// Set the color scheme on load
        /// </summary>
        protected override void OnLoaded()
        {
            switch (DialogSettings.ColorScheme)
            {
                case MetroDialogColorScheme.Accented:
                    PART_RestartButton.Style = FindResource("AccentedDialogHighlightedSquareButton") as Style;
                    PART_LaterButton.Style = FindResource("AccentedDialogHighlightedSquareButton") as Style;
                    break;
                case MetroDialogColorScheme.Theme:
                case MetroDialogColorScheme.Inverted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}