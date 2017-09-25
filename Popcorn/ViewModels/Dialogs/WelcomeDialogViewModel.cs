using System;
using System.IO;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace Popcorn.ViewModels.Dialogs
{
    public class WelcomeDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The close command
        /// </summary>
        private ICommand _closeCommand;

        /// <summary>
        /// Close action
        /// </summary>
        private readonly Action _closeAction;

        /// <summary>
        /// Help
        /// </summary>
        private string _welcome;

        /// <summary>
        /// Constructor
        /// </summary>
        public WelcomeDialogViewModel(Action closeAction)
        {
            _closeAction = closeAction;
            var subjectType = GetType();
            var subjectAssembly = subjectType.Assembly;
            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.Welcome.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        Welcome = reader.ReadToEnd();
                    }
                }
            }

            CloseCommand = new RelayCommand(() =>
            {
                _closeAction.Invoke();
            });
        }

        /// <summary>
        /// The close command
        /// </summary>
        public ICommand CloseCommand
        {
            get => _closeCommand;
            set { Set(() => CloseCommand, ref _closeCommand, value); }
        }

        /// <summary>
        /// Help
        /// </summary>
        public string Welcome
        {
            get => _welcome;
            set { Set(() => Welcome, ref _welcome, value); }
        }
    }
}
