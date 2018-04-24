using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Wraps an <see cref="T:System.Windows.Controls.AccordionItem" />.
    /// </summary>
    public class AccordionItemWrapperAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="item">The <see cref="T:System.Windows.Controls.AccordionItem" /> to wrap.</param>
        public AccordionItemWrapperAutomationPeer(AccordionItem item)
            : base(item)
        {
        }
    }
}
