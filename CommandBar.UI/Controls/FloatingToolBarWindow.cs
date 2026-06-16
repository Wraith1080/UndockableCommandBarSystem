using System.Windows;
using System.Windows.Input;

namespace CommandBar.UI.Controls
{
    public class FloatingToolBarWindow : Window
    {
        // NEW: A reference back to the original hidden toolbar
        public UndockableToolBar? OriginalToolBar { get; set; }

        public FloatingToolBarWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;
            Topmost = true;
            ShowInTaskbar = false;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();

                // NEW: After the user lets go of the mouse, check if we should dock
                OriginalToolBar?.CheckForRedock(this);
            }
        }
    }
}