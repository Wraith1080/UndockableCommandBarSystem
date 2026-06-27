using CommandBar.Core.Models;
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

        public FloatingToolBarWindow(CommandBar.Core.Models.ToolbarModel model)
        {
            this.DataContext = model; // 🟢 ADD THIS: Allows the XAML Title Bar to see the Name!
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            // 🟢 FIX BUG 2: Prevent tiny floating windows and spawn them safely!
            this.MinWidth = 120;
            this.MinHeight = 40;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            // ADD THESE 3 LINES: Strips the OS Titlebar and allows custom geometry
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            if (Application.Current.MainWindow != null)
            {
                this.Owner = Application.Current.MainWindow; // Keeps it above the main app!
            }

            // 🟢 FIX BUG 3: Listen to the Customize Dialog to close empty shells!
            model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CommandBar.Core.Models.ToolbarModel.IsVisible))
                {
                    if (model.IsVisible) this.Show();
                    else this.Hide();
                }
            };
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

                // 🟢 NEW: Save the physical location back to the Model!
                if (OriginalToolBar?.DataContext is ToolbarModel model)
                {
                    model.FloatingLeft = this.Left;
                    model.FloatingTop = this.Top;
                }

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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_TitleBar") is FrameworkElement titleBar)
            {
                titleBar.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ButtonState == MouseButtonState.Pressed)
                    {
                        // 🟢 FIX: Use our mathematical drag instead of the OS drag so it triggers redocking!
                        Point clickPos = e.GetPosition(this);
                        StartManualDrag(clickPos.X, clickPos.Y);
                    }
                };
            }

            if (GetTemplateChild("PART_CloseButton") is System.Windows.Controls.Primitives.ButtonBase closeBtn)
            {
                closeBtn.Click += (s, e) =>
                {
                    // 🟢 FIX: Restore the toolbar to its previous home!
                    if (this.DataContext is ToolbarModel model)
                    {
                        model.RequestDockChange(model.PreviousDockLocation);
                    }
                    this.Close();
                };
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 🟢 FIX: Clear the visibility binding before the window dies. 
            // This prevents WPF from throwing an exception if the user toggles the checkbox in the Customize menu later!
            System.Windows.Data.BindingOperations.ClearBinding(this, Window.VisibilityProperty);
            base.OnClosing(e);
        }
    }
}