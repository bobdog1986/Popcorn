using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     This type is used to determine the state of the item selected and the
    ///     previous items.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public enum RatingSelectionMode
    {
        /// <summary>
        ///     All items before the selected ones are selected.
        /// </summary>
        Continuous,

        /// <summary>
        ///     Only the item selected is visually distinguished.
        /// </summary>
        Individual
    }
}
