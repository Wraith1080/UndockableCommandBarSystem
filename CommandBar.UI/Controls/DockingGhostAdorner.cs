using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CommandBar.UI.Controls
{
    public class DockingGhostAdorner : Adorner
    {
        private readonly Rect _ghostRect;
        private readonly Pen _ghostPen;
        private readonly Brush _ghostBrush;

        public DockingGhostAdorner(UIElement adornedElement, Rect ghostRect)
            : base(adornedElement)
        {
            _ghostRect = ghostRect;

            // A semi-transparent blue fill with a dashed border (classic docking look)
            _ghostBrush = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215));
            _ghostPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 2)
            {
                DashStyle = DashStyles.Dash
            };

            // CRITICAL: We don't want the ghost intercepting the mouse and breaking our drag!
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw the ghost rectangle onto the adorner layer
            drawingContext.DrawRectangle(_ghostBrush, _ghostPen, _ghostRect);
        }
    }
}