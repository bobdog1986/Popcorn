using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
        private static bool _isSeeking;
        private static SemaphoreSlim PositionSemaphore = new SemaphoreSlim(1, 1);

        public object Convert(
            object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (PositionSemaphore.CurrentCount == 0) return _oldValue;
            if (!(values[0] is TimeSpan playerDuration) || !(values[1] is bool isSeeking) ||
                !(values[2] is double naturalDuration) ||
                !(values[3] is double progress) || !(values[4] is MediaType mediaType))
                return 0d;
            _progress = progress / 100d;
            _mediaType = mediaType;
            _isSeeking = isSeeking;
            _naturalDuration = naturalDuration;
            if (isSeeking || Math.Abs(playerDuration.TotalSeconds - _oldValue) > 1d) return _oldValue;
            _oldValue = playerDuration.TotalSeconds;
            return playerDuration.TotalSeconds;
        }

        public object[] ConvertBack(
            object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            PositionSemaphore.Wait();
            var result = TimeSpan.FromTicks((long) Math.Round(TimeSpan.TicksPerSecond * (double) value, 0));
            if (_mediaType != MediaType.Trailer && _naturalDuration != 0d &&
                result.TotalSeconds / _naturalDuration >= _progress + _progress * 0.05d)
            {
                var oldResult = TimeSpan.FromTicks((long) Math.Round(TimeSpan.TicksPerSecond * (double) _oldValue, 0));
                _oldValue = oldResult.TotalSeconds;
                PositionSemaphore.Release();
                return new object[] {oldResult, _isSeeking, oldResult.TotalSeconds, _progress, _mediaType};
            }

            _oldValue = result.TotalSeconds;
            PositionSemaphore.Release();
            return new object[] {result, _isSeeking, result.TotalSeconds, _progress, _mediaType};
        }
    }
}