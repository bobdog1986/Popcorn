using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Exposes Rating types to UI Automation.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public sealed class RatingAutomationPeer : ItemsControlAutomationPeer, IValueProvider, ISelectionProvider
    {
#if !SILVERLIGHT
        /// <summary>
        ///     Provides initialization for base class values when called by the constructor
        ///     of a derived class.
        /// </summary>
        /// <param name="item">The item to create the automation peer for.</param>
        /// <returns>The item automation peer.</returns>
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            ItemAutomationPeer peer = null;
            var element = item as UIElement;
            if (element != null)
            {
                peer = CreatePeerForElement(element) as ItemAutomationPeer;
            }
            return peer;
        }
#endif

        /// <summary>
        ///     Gets the Rating that owns this RatingAutomationPeer.
        /// </summary>
        /// <value>The Rating.</value>
        private Rating OwnerRating
        {
            get { return (Rating)Owner; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RatingAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">
        ///     The Rating that is associated with this
        ///     RatingAutomationPeer.
        /// </param>
        public RatingAutomationPeer(Rating owner) : base(owner)
        {
        }

        /// <summary>
        ///     Returns a name if no name is set.
        /// </summary>
        /// <returns>A name if no name is set.</returns>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (string.IsNullOrEmpty(name))
            {
                return "Rating";
            }
            return name;
        }

        /// <summary>
        ///     Returns the localized control type.
        /// </summary>
        /// <returns>The localized control type.</returns>
        protected override string GetLocalizedControlTypeCore()
        {
            return "Rating";
        }

        /// <summary>
        ///     Gets the control type for the Rating that is associated
        ///     with this RatingAutomationPeer.  This method is called by
        ///     GetAutomationControlType.
        /// </summary>
        /// <returns>List AutomationControlType.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Slider;
        }

        /// <summary>
        ///     Gets the control pattern for the Rating that is associated
        ///     with this RatingAutomationPeer.
        /// </summary>
        /// <param name="patternInterface">The desired PatternInterface.</param>
        /// <returns>The desired AutomationPeer or null.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value ||
                patternInterface == PatternInterface.Selection)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <summary>
        ///     Gets the collection of child elements of
        ///     the <see cref="T:System.Windows.Controls.ItemsControl" /> that is
        ///     associated with this <see cref="T:System.Windows.Automation.Peers.ItemsControlAutomationPeer" />.
        /// </summary>
        /// <returns>
        ///     A collection of RatingItemAutomationPeer elements, or null if the
        ///     Rating that is associated with this RatingAutomationPeer is
        ///     empty.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Required by automation")
        ]
        protected override List<AutomationPeer> GetChildrenCore()
        {
            Rating owner = OwnerRating;

            ItemCollection items = owner.Items;
            if (items.Count <= 0)
            {
                return null;
            }

            var peers = new List<AutomationPeer>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var element = owner.ItemContainerGenerator.ContainerFromIndex(i) as RatingItem;
                if (element != null)
                {
                    peers.Add(FromElement(element) ?? CreatePeerForElement(element));
                }
            }
            return peers;
        }

        /// <summary>
        ///     Gets a value indicating whether the UI Automation provider
        ///     allows more than one child element to be selected concurrently.
        /// </summary>
        /// <returns>
        ///     true if multiple selection is allowed; otherwise, false.
        /// </returns>
        /// <remarks>
        ///     This API supports the .NET Framework infrastructure and is not
        ///     intended to be used directly from your code.
        /// </remarks>
        public bool CanSelectMultiple
        {
            get { return false; }
        }

        /// <summary>
        ///     Retrieves a UI Automation provider for each child element that is
        ///     selected.
        /// </summary>
        /// <returns>An array of UI Automation providers.</returns>
        /// <remarks>
        ///     This API supports the .NET Framework infrastructure and is not
        ///     intended to be used directly from your code.
        /// </remarks>
        public IRawElementProviderSimple[] GetSelection()
        {
            RatingItem selectedRatingItem =
                OwnerRating.GetRatingItems().LastOrDefault(ratingItem => ratingItem.Value > 0.0);
            if (selectedRatingItem != null)
            {
                return new[] { ProviderFromPeer(FromElement(selectedRatingItem)) };
            }
            return new IRawElementProviderSimple[] { };
        }

        /// <summary>
        ///     Gets a value indicating whether the UI Automation provider
        ///     requires at least one child element to be selected.
        /// </summary>
        /// <returns>
        ///     true if selection is required; otherwise, false.
        /// </returns>
        /// <remarks>
        ///     This API supports the .NET Framework infrastructure and is not
        ///     intended to be used directly from your code.
        /// </remarks>
        public bool IsSelectionRequired
        {
            get { return false; }
        }

        /// <summary>
        ///     Gets a value indicating whether the Rating is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return OwnerRating.IsReadOnly; }
        }

        /// <summary>
        ///     Sets a rating value.
        /// </summary>
        /// <param name="value">The value of the rating.</param>
        public void SetValue(string value)
        {
            double ratingValue;
            if (string.IsNullOrEmpty(value))
            {
                OwnerRating.Value = null;
            }
            else if (double.TryParse(value, out ratingValue))
            {
                if (ratingValue < 0.0 || ratingValue > 1.0)
                {
                    throw new InvalidOperationException("Value must be null or a number between 0 and 1.");
                }
                OwnerRating.Value = ratingValue;
            }
            else
            {
                throw new InvalidOperationException("Value must be null or a number between 0 and 1.");
            }
        }

        /// <summary>
        ///     Gets the rating value.
        /// </summary>
        public string Value
        {
            get
            {
                if (OwnerRating.Value.HasValue)
                {
                    return OwnerRating.Value.ToString();
                }
                return null;
            }
        }
    }
}
