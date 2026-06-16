using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommandBar.UI.Controls
{
    public class UndockableToolBar : ItemsControl
    {
        private UIElement? _dragGrip;
        private bool _isDragging;
        private Point _startMousePosition;

        public static readonly DependencyProperty TearOffOffsetXProperty =
            DependencyProperty.Register(nameof(TearOffOffsetX), typeof(double), typeof(UndockableToolBar), new PropertyMetadata(10.0));

        public static readonly DependencyProperty TearOffOffsetYProperty =
            DependencyProperty.Register(nameof(TearOffOffsetY), typeof(double), typeof(UndockableToolBar), new PropertyMetadata(10.0));

        public double TearOffOffsetX
        {
            get => (double)GetValue(TearOffOffsetXProperty);
            set => SetValue(TearOffOffsetXProperty, value);
        }

        public double TearOffOffsetY
        {
            get => (double)GetValue(TearOffOffsetYProperty);
            set => SetValue(TearOffOffsetYProperty, value);
        }

        static UndockableToolBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(UndockableToolBar),
                new FrameworkPropertyMetadata(typeof(UndockableToolBar)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _dragGrip = GetTemplateChild("PART_DragGrip") as UIElement;

            if (_dragGrip != null)
            {
                _dragGrip.MouseLeftButtonDown += DragGrip_MouseLeftButtonDown;
                _dragGrip.MouseMove += DragGrip_MouseMove;
                _dragGrip.MouseLeftButtonUp += DragGrip_MouseLeftButtonUp;
            }
        }

        private void DragGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow is FloatingToolBarWindow floatingWindow)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    floatingWindow.DragMove(); // Blocks until mouse is released

                    // NEW: Check if the user dropped the window over a docking zone
                    floatingWindow.OriginalToolBar?.CheckForRedock(floatingWindow);
                }
                return;
            }

            _isDragging = true;
            _startMousePosition = e.GetPosition(this);
            _dragGrip?.CaptureMouse();
        }

        private void DragGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point currentPosition = e.GetPosition(this);
            Vector diff = currentPosition - _startMousePosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = false;
                _dragGrip?.ReleaseMouseCapture();

                InitiateTearOff();
            }
        }

        private void DragGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _dragGrip?.ReleaseMouseCapture();
        }

        private void InitiateTearOff()
        {
            var floatingWindow = new FloatingToolBarWindow();

            // NEW: Give the floating window a reference to this original toolbar
            floatingWindow.OriginalToolBar = this;

            var floatingBar = new UndockableToolBar
            {
                ItemsSource = this.ItemsSource
            };

            floatingWindow.Content = floatingBar;

            var mousePos = PointToScreen(Mouse.GetPosition(this));
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                mousePos = source.CompositionTarget.TransformFromDevice.Transform(mousePos);
            }

            floatingWindow.Left = mousePos.X - TearOffOffsetX;
            floatingWindow.Top = mousePos.Y - TearOffOffsetY;

            this.Visibility = Visibility.Collapsed;

            floatingWindow.Show();

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                floatingWindow.DragMove();

                // NEW: If they immediately drop it after tearing off, check for redock
                CheckForRedock(floatingWindow);
            }
        }

        // NEW METHOD: The Hit-Testing Logic
        public void CheckForRedock(FloatingToolBarWindow floatingWindow)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow == null) return;

            // Get the mouse cursor's exact position relative to the main application window
            Point mousePos = Mouse.GetPosition(mainWindow);

            // Define our target "Docking Zone" (e.g., the top 50 pixels of the main window)
            bool isOverDockZone = mousePos.Y >= -20 && mousePos.Y <= 50 &&
                                  mousePos.X >= -20 && mousePos.X <= mainWindow.ActualWidth + 20;

            if (isOverDockZone)
            {
                // Snap it back! Destroy the floating window and reveal the original.
                floatingWindow.Close();
                this.Visibility = Visibility.Visible;
            }
        }
    }
}