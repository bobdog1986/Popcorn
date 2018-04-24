using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Defines the minimum and maximum number of selected items allowed in an Accordion control.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public enum AccordionSelectionMode
    {
        /// <summary>
        ///     Exactly one item must be selected in the Accordion.
        /// </summary>
        One,

        /// <summary>
        ///     At least one item must be selected in the Accordion.
        /// </summary>
        OneOrMore,

        /// <summary>
        ///     No more than one item can be selected in the accordion.
        /// </summary>
        ZeroOrOne,

        /// <summary>
        ///     Any number of  items can be selected in the Accordion.
        /// </summary>
        ZeroOrMore
    }
}
