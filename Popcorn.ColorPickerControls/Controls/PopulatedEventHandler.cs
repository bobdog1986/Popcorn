using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Represents the method that will handle the
    ///     <see cref="E:System.Windows.Controls.AutoCompleteBox.Populated" />
    ///     event of a <see cref="T:System.Windows.Controls.AutoCompleteBox" />
    ///     control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">
    ///     A
    ///     <see cref="T:System.Windows.Controls.PopulatedEventArgs" /> that
    ///     contains the event data.
    /// </param>
    /// <QualityBand>Stable</QualityBand>
    [SuppressMessage("Microsoft.Design", "CA1003:UseGenericEventHandlerInstances",
        Justification = "There is no generic RoutedEventHandler.")]
    public delegate void PopulatedEventHandler(object sender, PopulatedEventArgs e);
}
