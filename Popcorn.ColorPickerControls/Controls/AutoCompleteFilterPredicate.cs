using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Represents the filter used by the
    ///     <see cref="T:System.Windows.Controls.AutoCompleteBox" /> control to
    ///     determine whether an item is a possible match for the specified text.
    /// </summary>
    /// <returns>
    ///     true to indicate <paramref name="item" /> is a possible match
    ///     for <paramref name="search" />; otherwise false.
    /// </returns>
    /// <param name="search">The string used as the basis for filtering.</param>
    /// <param name="item">
    ///     The item that is compared with the
    ///     <paramref name="search" /> parameter.
    /// </param>
    /// <typeparam name="T">
    ///     The type used for filtering the
    ///     <see cref="T:System.Windows.Controls.AutoCompleteBox" />. This type can
    ///     be either a string or an object.
    /// </typeparam>
    /// <QualityBand>Stable</QualityBand>
    public delegate bool AutoCompleteFilterPredicate<T>(string search, T item);
}
