using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CommandBar.UI.Controls
{
    public class FloatingToolBarWindow : Window
    {
        public UndockableToolBar? OriginalToolBar { get; set; }

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        static FloatingToolBarWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatingToolBarWindow),
                new FrameworkPropertyMetadata(typeof(FloatingToolBarWindow)));
        }

        public FloatingToolBarWindow()
        {
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);

            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }
            return IntPtr.Zero;
        }

        // --- THE UNIFIED WPF DRAG ENGINE ---

        private bool _isManualDragging;
        private double _dragOffsetX;
        private double _dragOffsetY;

        public void StartManualDrag(double offsetX, double offsetY)
        {
            _isManualDragging = true;
            _dragOffsetX = offsetX;
            _dragOffsetY = offsetY;
            this.CaptureMouse(); // Lock mouse events to this window
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                // NO MORE WIN32 DRAG! Find where they clicked and start the math drag
                Point clickPos = e.GetPosition(this);
                StartManualDrag(clickPos.X, clickPos.Y);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isManualDragging)
            {
                GetCursorPos(out POINT p);
                Point logicalPos = new Point(p.X, p.Y);
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    logicalPos = source.CompositionTarget.TransformFromDevice.Transform(logicalPos);
                }

                this.Left = logicalPos.X - _dragOffsetX;
                this.Top = logicalPos.Y - _dragOffsetY;

                OriginalToolBar?.UpdateGhostAdorner(this);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isManualDragging)
            {
                // Drag is finished! Release mouse and check if we should dock.
                _isManualDragging = false;
                this.ReleaseMouseCapture();

                OriginalToolBar?.ClearGhostAdorner();
                OriginalToolBar?.CheckForRedock(this);
            }
        }
    }
}