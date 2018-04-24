using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Popcorn.ColorPickerControls.Controls
{
    /// <summary>
    ///     Clips the content of the control in a given direction.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public class LinearClipper : Clipper
    {
        #region public ExpandDirection ExpandDirection

        /// <summary>
        ///     Gets or sets the clipped edge.
        /// </summary>
        public ExpandDirection ExpandDirection
        {
            get { return (ExpandDirection)GetValue(ExpandDirectionProperty); }
            set { SetValue(ExpandDirectionProperty, value); }
        }

        /// <summary>
        ///     Identifies the ExpandDirection dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpandDirectionProperty =
            DependencyProperty.Register(
                "ExpandDirection",
                typeof(ExpandDirection),
                typeof(LinearClipper),
                new PropertyMetadata(ExpandDirection.Right, OnExpandDirectionChanged));

        /// <summary>
        ///     ExpandDirectionProperty property changed handler.
        /// </summary>
        /// <param name="d">ExpandDirectionView that changed its ExpandDirection.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnExpandDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (LinearClipper)d;
            var oldValue = (ExpandDirection)e.OldValue;
            var newValue = (ExpandDirection)e.NewValue;
            source.OnExpandDirectionChanged(oldValue, newValue);
        }

        /// <summary>
        ///     ExpandDirectionProperty property changed handler.
        /// </summary>
        /// <param name="oldValue">Old value.</param>
        /// <param name="newValue">New value.</param>
        protected virtual void OnExpandDirectionChanged(ExpandDirection oldValue, ExpandDirection newValue)
        {
            ClipContent();
        }

        #endregion public ExpandDirection ExpandDirection

        /// <summary>
        ///     Updates the clip geometry.
        /// </summary>
        protected override void ClipContent()
        {
            if (ExpandDirection == ExpandDirection.Right)
            {
                double width = RenderSize.Width * RatioVisible;
                Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, RenderSize.Height) };
            }
            else if (ExpandDirection == ExpandDirection.Left)
            {
                double width = RenderSize.Width * RatioVisible;
                double rightSide = RenderSize.Width - width;
                Clip = new RectangleGeometry { Rect = new Rect(rightSide, 0, width, RenderSize.Height) };
            }
            else if (ExpandDirection == ExpandDirection.Up)
            {
                double height = RenderSize.Height * RatioVisible;
                double bottom = RenderSize.Height - height;
                Clip = new RectangleGeometry { Rect = new Rect(0, bottom, RenderSize.Width, height) };
            }
            else if (ExpandDirection == ExpandDirection.Down)
            {
                double height = RenderSize.Height * RatioVisible;
                Clip = new RectangleGeometry { Rect = new Rect(0, 0, RenderSize.Width, height) };
            }
        }
    }
}
