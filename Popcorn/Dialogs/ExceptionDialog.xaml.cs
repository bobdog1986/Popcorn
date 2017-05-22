using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Manage exception settings
    /// </summary>
    public class ExceptionDialogSettings : MetroDialogSettings
    {
        /// <summary>
        /// Initialize a new instance of ExceptionDialogSettings
        /// </summary>
        /// <param name="title">The dialog title</param>
        /// <param name="message">The dialog message</param>
        public ExceptionDialogSettings(string title, string message)
        {
            Title = title;
            Message = message;
        }

        /// <summary>
        /// Dialog title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Dialog message
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    /// Manage exception dialog
    /// </summary>
    public partial class ExceptionDialog
    {
        /// <summary>
        /// Message property
        /// </summary>
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message",
            typeof (string), typeof (ExceptionDialog), new PropertyMetadata(default(string)));

        /// <summary>
        /// Ok button property
        /// </summary>
        public static readonly DependencyProperty OkButtonTextProperty = DependencyProperty.Register("OkButtonText",
            typeof (string), typeof (ExceptionDialog), new PropertyMetadata("Ok"));

        /// <summary>
        /// Initialize a new instance of ExceptionDialog
        /// </summary>
        /// <param name="settings">The dialog settings</param>
        internal ExceptionDialog(ExceptionDialogSettings settings)
        {
            InitializeComponent();
            Message = settings.Message;
            Title = settings.Title;
        }

        /// <summary>
        /// Dialog message
        /// </summary>
        public string Message
        {
            get => (string) GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Ok button content
        /// </summary>
        public string OkButtonText
        {
            get => (string) GetValue(OkButtonTextProperty);
            set => SetValue(OkButtonTextProperty, value);
        }

        /// <summary>
        /// Asynchronous task, waiting for button press event to complete
        /// </summary>
        /// <returns></returns>
        internal Task WaitForButtonPressAsync()
        {
            var tcs = new TaskCompletionSource<object>();

            RoutedEventHandler okHandler = null;
            KeyEventHandler okKeyHandler = null;

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

                PART_OkButton.Click -= okHandler;

                PART_OkButton.KeyDown -= okKeyHandler;

                cancellationTokenRegistration.Dispose();
            };

            escapeKeyHandler = (sender, e) =>
            {
                if (e.Key != Key.Escape) return;
                cleanUpHandlers();

                tcs.TrySetResult(null);
            };

            okKeyHandler = (sender, e) =>
            {
                if (e.Key != Key.Enter) return;
                cleanUpHandlers();

                tcs.TrySetResult(null);
            };

            okHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(null);

                e.Handled = true;
            };

            PART_OkButton.KeyDown += okKeyHandler;

            KeyDown += escapeKeyHandler;

            PART_OkButton.Click += okHandler;

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
                    PART_OkButton.Style = FindResource("AccentedDialogHighlightedSquareButton") as Style;
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