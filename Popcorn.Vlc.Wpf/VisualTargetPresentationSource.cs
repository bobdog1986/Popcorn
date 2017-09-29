using System;
using System.Windows;
using System.Windows.Media;

namespace Popcorn.Vlc.Wpf
{
    public class VisualTargetPresentationSource : PresentationSource, IDisposable
    {
        private readonly VisualTarget _visualTarget;

        private bool _isDisposed;

        public VisualTargetPresentationSource(HostVisual hostVisual)
        {
            _visualTarget = new VisualTarget(hostVisual);
            AddSource();
        }

        public override Visual RootVisual
        {
            get
            {
                try
                {
                    return _visualTarget.RootVisual;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            set
            {
                try
                {
                    Visual oldRoot = _visualTarget.RootVisual;
                    _visualTarget.RootVisual = value;
                    RootChanged(oldRoot, value);

                    UIElement rootElement = value as UIElement;
                    if (rootElement != null)
                    {
                        rootElement.Measure(new System.Windows.Size(Double.PositiveInfinity, Double.PositiveInfinity));
                        rootElement.Arrange(new Rect(rootElement.DesiredSize));
                    }
                }
                catch (Exception) { }
            }
        }

        public override bool IsDisposed
        {
            get { return _isDisposed; }
        }

        public void Dispose()
        {
            RemoveSource();
            _isDisposed = true;
        }

        protected override CompositionTarget GetCompositionTargetCore()
        {
            return _visualTarget;
        }
    }
}