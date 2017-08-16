using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace Popcorn.Models.Subtitles
{
    public class SubtitleSize : ObservableObject
    {
        private string _label;
        public string Label
        {
            get => _label;
            set => Set(ref _label, value);
        }

        private int _size;
        public int Size
        {
            get => _size;
            set => Set(ref _size, value);
        }
    }
}
