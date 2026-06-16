using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CommandBar.UI.Controls
{
    public class InsertionCaretAdorner : Adorner
    {
        private Point _insertionPoint;
        private readonly Pen _caretPen;

        public InsertionCaretAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false; // Never let the caret block the mouse!

            // A crisp, 2-pixel wide blue line
            _caretPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 2);
        }

        public void UpdatePosition(Point newPoint)
        {
            _insertionPoint = newPoint;
            InvalidateVisual(); // Forces WPF to redraw the Adorner
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw a vertical line from the top of the toolbar to the bottom
            drawingContext.DrawLine(_caretPen,
                new Point(_insertionPoint.X, 2),
                new Point(_insertionPoint.X, ActualHeight - 2));
        }
    }
}