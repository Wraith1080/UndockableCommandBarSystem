using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommandBar.Core.Models; // Required for DockLocation

namespace CommandBar.UI.Controls
{
    public class DockingGhostAdorner : Adorner
    {
        private DockLocation _targetDock = DockLocation.Top;

        public DockingGhostAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        // NEW: Allows the Drag Loop to update the visual rotation on the fly
        public void UpdateTargetDock(DockLocation dock)
        {
            if (_targetDock != dock)
            {
                _targetDock = dock;
                InvalidateVisual(); // Forces a redraw!
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // A sleek, semi-transparent blue rectangle
            var brush = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215));
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 2);
            pen.DashStyle = DashStyles.Dash;

            double width, height;
            double rectX = 0, rectY = 0;

            // NEW MATH: Determine dimensions and position based on the edge!
            if (_targetDock == DockLocation.Left || _targetDock == DockLocation.Right)
            {
                // Vertical Strip
                width = 40;
                height = this.AdornedElement.RenderSize.Height;

                rectX = _targetDock == DockLocation.Left ? 0 : this.AdornedElement.RenderSize.Width - width;
                rectY = 0;
            }
            else
            {
                // Horizontal Strip
                width = this.AdornedElement.RenderSize.Width;
                height = 40;

                rectX = 0;
                rectY = _targetDock == DockLocation.Top ? 0 : this.AdornedElement.RenderSize.Height - height;
            }

            drawingContext.DrawRectangle(brush, pen, new Rect(rectX, rectY, width, height));
        }
    }
}