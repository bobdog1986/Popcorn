using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Popcorn.Utils;

namespace Popcorn.Converters
{
    public class ProgressToPositionConverter : IMultiValueConverter
    {
        private static double _progress;
        private static double _naturalDuration;
        private static double _oldValue;
        private static MediaType _mediaType;

        public object Convert(
            object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(values[0] is TimeSpan playerDuration) || !(values[1] is double naturalDuration) ||
                !(values[2] is double progress) || !(values[3] is MediaType mediaType))
                return 0d;
            _progress = progress / 100d;
            _mediaType = mediaType;
            _naturalDuration = naturalDuration;
            _oldValue = playerDuration.TotalSeconds;
            return playerDuration.TotalSeconds;
        }

        public object[] ConvertBack(
            object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var result = TimeSpan.FromTicks((long) Math.Round(TimeSpan.TicksPerSecond * (double) value, 0));
            if (_mediaType != MediaType.Trailer && _naturalDuration != 0d &&
                result.TotalSeconds / _naturalDuration >= _progress + _progress * 0.05d)
            {
                var oldResult = TimeSpan.FromTicks((long) Math.Round(TimeSpan.TicksPerSecond * (double) _oldValue, 0));
                return new object[] {oldResult, oldResult.TotalSeconds};
            }

            return new object[] {result, result.TotalSeconds};
        }
    }
}