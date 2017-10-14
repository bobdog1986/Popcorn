using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Popcorn.Controls;

namespace Popcorn.UserControls.Home.Movie.Details
{
    /// <summary>
    /// Interaction logic for Movie.xaml
    /// </summary>
    public partial class MovieDetailsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the Movie class.
        /// </summary>
        public MovieDetailsUserControl()
        {
            InitializeComponent();
        }

        private void OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scv = (AnimatedScrollViewer) sender;
            if (scv.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            {
                scv.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                return;
            }

            scv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            if (scv.TargetHorizontalOffset - e.Delta >= -Math.Abs(e.Delta) &&
                scv.TargetHorizontalOffset - e.Delta < scv.ScrollableWidth + Math.Abs(e.Delta))
            {
                scv.TargetHorizontalOffset -= e.Delta;
            }
        }
    }
}