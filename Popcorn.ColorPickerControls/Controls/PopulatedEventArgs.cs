using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Provides data for the
    ///     <see cref="E:System.Windows.Controls.AutoCompleteBox.Populated" />
    ///     event.
    /// </summary>
    /// <QualityBand>Stable</QualityBand>
    public class PopulatedEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///     Gets the list of possible matches added to the drop-down portion of
        ///     the <see cref="T:System.Windows.Controls.AutoCompleteBox" />
        ///     control.
        /// </summary>
        /// <value>
        ///     The list of possible matches added to the
        ///     <see cref="T:System.Windows.Controls.AutoCompleteBox" />.
        /// </value>
        public IEnumerable Data { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="T:System.Windows.Controls.PopulatedEventArgs" />.
        /// </summary>
        /// <param name="data">
        ///     The list of possible matches added to the
        ///     drop-down portion of the
        ///     <see cref="T:System.Windows.Controls.AutoCompleteBox" /> control.
        /// </param>
        public PopulatedEventArgs(IEnumerable data)
        {
            Data = data;
        }

#if !SILVERLIGHT
        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="T:System.Windows.Controls.PopulatedEventArgs" />.
        /// </summary>
        /// <param name="data">
        ///     The list of possible matches added to the
        ///     drop-down portion of the
        ///     <see cref="T:System.Windows.Controls.AutoCompleteBox" /> control.
        /// </param>
        /// <param name="routedEvent">The routed event identifier for this instance.</param>
        public PopulatedEventArgs(IEnumerable data, RoutedEvent routedEvent)
            : base(routedEvent)
        {
            Data = data;
        }
#endif
    }
}
