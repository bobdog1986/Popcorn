using System;
using System.IO;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace Popcorn.ViewModels.Dialogs
{
    public class AboutDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The close command
        /// </summary>
        private ICommand _closeCommand;

        /// <summary>
        /// License
        /// </summary>
        private string _license;

        /// <summary>
        /// Versions
        /// </summary>
        private string _versionDescription;

        /// <summary>
        /// About
        /// </summary>
        private string _about;

        /// <summary>
        /// Close action
        /// </summary>
        private readonly Action _closeAction;

        /// <summary>
        /// Constructor
        /// </summary>
        public AboutDialogViewModel(Action closeAction)
        {
            _closeAction = closeAction;
            var subjectType = GetType();
            var subjectAssembly = subjectType.Assembly;
            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.Versions.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        VersionDescription = reader.ReadToEnd();
                    }
                }
            }

            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.License.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        License = reader.ReadToEnd();
                    }
                }
            }

            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.About.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        About = reader.ReadToEnd();
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
        /// About
        /// </summary>
        public string About
        {
            get => _about;
            set { Set(() => About, ref _about, value); }
        }

        /// <summary>
        /// The license
        /// </summary>
        public string License
        {
            get => _license;
            set { Set(() => License, ref _license, value); }
        }

        /// <summary>
        /// The version description
        /// </summary>
        public string VersionDescription
        {
            get => _versionDescription;
            set { Set(() => VersionDescription, ref _versionDescription, value); }
        }
    }
}
