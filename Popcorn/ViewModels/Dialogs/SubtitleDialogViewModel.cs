using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Popcorn.Models.Subtitles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Popcorn.Helpers;

namespace Popcorn.ViewModels.Dialogs
{
    public class SubtitleDialogViewModel : ViewModelBase
    {
        public Action OnCloseAction { get; set; }

        private ObservableCollection<Subtitle> _availableSubtitles;

        private Subtitle _selectedSubtitle;

        private ICommand _closeCommand;

        public SubtitleDialogViewModel(IEnumerable<Subtitle> subtitles, OSDB.Subtitle currentSubtitle)
        {
            AvailableSubtitles = new ObservableCollection<Subtitle>(subtitles ?? new List<Subtitle>());
            if (currentSubtitle != null)
            {
                SelectedSubtitle =
                    AvailableSubtitles.FirstOrDefault(a => a.Sub.LanguageId == currentSubtitle.LanguageId);
            }
            else
            {
                SelectedSubtitle =
                    AvailableSubtitles.FirstOrDefault(a => a.Sub.LanguageName ==
                                                           LocalizationProviderHelper.GetLocalizedValue<string>(
                                                               "NoneLabel"));
            }

            CloseCommand = new RelayCommand(() =>
             {
                 OnCloseAction.Invoke();
             });
        }

        public Subtitle SelectedSubtitle
        {
            get => _selectedSubtitle;
            set => Set(ref _selectedSubtitle, value);
        }

        public ObservableCollection<Subtitle> AvailableSubtitles
        {
            get => _availableSubtitles;
            set => Set(ref _availableSubtitles, value);
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => Set(ref _closeCommand, value);
        }
    }
}