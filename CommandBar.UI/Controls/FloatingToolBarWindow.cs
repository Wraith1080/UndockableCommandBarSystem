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

        // Existing Mouse Constants
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        // NEW: Window Style Constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        // NEW: Win32 API Imports
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // --- 1. NEW NATIVE DRAG IMPORTS ---
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public FloatingToolBarWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;

            // NEW: Prevent WPF from even trying to put keyboard focus in this window
            Focusable = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // NEW: Get the raw OS window handle (HWND)
            var hwnd = new WindowInteropHelper(this).Handle;

            // Apply the WS_EX_NOACTIVATE extended style
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);

            // Keep the mouse hook as a backup for internal WPF routing
            var source = PresentationSource.FromVisual(this) as HwndSource;
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

        // --- 2. NEW SILENT DRAG ENGINE ---
        public void NoActivateDragMove()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            ReleaseCapture(); // Drop WPF's mouse capture
            // Trick the OS into thinking we clicked the title bar natively
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero); 
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                // 3. REPLACE WPF's DragMove() WITH OUR CUSTOM METHOD
                NoActivateDragMove(); 
                OriginalToolBar?.CheckForRedock(this);
            }
        }
    }
}