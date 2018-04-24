using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Converts a string or base value to a <see cref="Nullable" /> value.
    /// </summary>
    /// <typeparam name="T">The type should be value type.</typeparam>
    /// <QualityBand>Preview</QualityBand>
    public class NullableConverter<T> : TypeConverter where T : struct
    {
        /// <summary>
        ///     Returns whether the type converter can convert an object from the
        ///     specified type to the type of this converter.
        /// </summary>
        /// <param name="context">
        ///     An object that provides a format context.
        /// </param>
        /// <param name="sourceType">The type you want to convert from.</param>
        /// <returns>
        ///     Returns true if this converter can perform the conversion;
        ///     otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(T))
            {
                return true;
            }
            if (sourceType == typeof(string))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Returns whether the type converter can convert an object from the
        ///     specified type to the type of this converter.
        /// </summary>
        /// <param name="context">
        ///     An object that provides a format context.
        /// </param>
        /// <param name="destinationType">
        ///     The type you want to convert to.
        /// </param>
        /// <returns>
        ///     Returns true if this converter can perform the conversion;
        ///     otherwise, false.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(T));
        }

        /// <summary>
        ///     Converts from the specified value to the type of this converter.
        /// </summary>
        /// <param name="context">
        ///     An object that provides a format context.
        /// </param>
        /// <param name="culture">
        ///     The
        ///     <see cref="T:System.Globalization.CultureInfo" /> to use as the
        ///     current culture.
        /// </param>
        /// <param name="value">
        ///     The value to convert to the type of this
        ///     converter.
        /// </param>
        /// <returns>The converted value.</returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     The conversion cannot be performed.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = value as string;
            if (value is T)
            {
                return (T)value;
            }
            if (string.IsNullOrEmpty(stringValue) ||
                String.Equals(stringValue, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return new T?();
            }
            return (T)Convert.ChangeType(value, typeof(T), culture);
        }

        /// <summary>
        ///     Converts from the specified value to the a specified type from the
        ///     type of this converter.
        /// </summary>
        /// <param name="context">
        ///     An object that provides a format context.
        /// </param>
        /// <param name="culture">
        ///     The
        ///     <see cref="T:System.Globalization.CultureInfo" /> to use as the
        ///     current culture.
        /// </param>
        /// <param name="value">
        ///     The value to convert to the type of this
        ///     converter.
        /// </param>
        /// <param name="destinationType">
        ///     The type of convert the value to
        ///     .
        /// </param>
        /// <returns>The converted value.</returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     The conversion cannot be performed.
        /// </exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (value == null)
            {
                return string.Empty;
            }
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
