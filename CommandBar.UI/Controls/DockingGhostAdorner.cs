using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommandBar.Core.Models;

namespace CommandBar.UI.Controls
{
    public class DockingGhostAdorner : Adorner
    {
        private DockLocation _targetDock = DockLocation.Top;

        // NEW: Public properties replace the magic numbers and colors!
        public double GhostThickness { get; set; } = 40.0;
        public Brush GhostFill { get; set; } = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215));
        public Pen GhostPen { get; set; } = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 2) { DashStyle = DashStyles.Dash };

        public DockingGhostAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        public void UpdateTargetDock(DockLocation dock)
        {
            if (_targetDock != dock)
            {
                _targetDock = dock;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double width, height;
            double rectX = 0, rectY = 0;

            // NEW: Using the GhostThickness property instead of hardcoded '40'
            if (_targetDock == DockLocation.Left || _targetDock == DockLocation.Right)
            {
                width = GhostThickness;
                height = this.AdornedElement.RenderSize.Height;

                rectX = _targetDock == DockLocation.Left ? 0 : this.AdornedElement.RenderSize.Width - width;
                rectY = 0;
            }
            else
            {
                width = this.AdornedElement.RenderSize.Width;
                height = GhostThickness;

                rectX = 0;
                rectY = _targetDock == DockLocation.Top ? 0 : this.AdornedElement.RenderSize.Height - height;
            }

            // NEW: Using the exposed brush and pen properties
            drawingContext.DrawRectangle(GhostFill, GhostPen, new Rect(rectX, rectY, width, height));
        }
    }
}