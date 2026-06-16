using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;

namespace CommandBar.UI.Controls
{
    public class UndockableToolBar : ToolBar
    {
        private UIElement? _dragGrip;
        private bool _isDragging;
        private Point _startMousePosition;

        // NEW: Keep track of the active ghost so we can remove it later
        private DockingGhostAdorner? _currentAdorner;

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
                    floatingWindow.LocationChanged += FloatingWindow_LocationChanged;
                    
                    floatingWindow.DragMove(); 
                    
                    floatingWindow.LocationChanged -= FloatingWindow_LocationChanged;
                    
                    // NEW: Ask the ORIGINAL toolbar to clear the ghost!
                    floatingWindow.OriginalToolBar?.ClearGhostAdorner();

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
                // NEW: Wire up the initial tear-off drag as well
                floatingWindow.LocationChanged += FloatingWindow_LocationChanged;

                floatingWindow.DragMove();

                floatingWindow.LocationChanged -= FloatingWindow_LocationChanged;
                ClearGhostAdorner();
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

        // NEW: Event handler that fires continuously while the floating window is dragged
        private void FloatingWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (sender is FloatingToolBarWindow floatingWindow && floatingWindow.OriginalToolBar != null)
            {
                floatingWindow.OriginalToolBar.UpdateGhostAdorner(floatingWindow);
            }
        }

        public void UpdateGhostAdorner(FloatingToolBarWindow floatingWindow)
        {
            var mainWindow = Window.GetWindow(this);
            // NEW: Get the root visual element inside the main window
            if (mainWindow == null || mainWindow.Content is not UIElement rootElement) return;

            Point mousePos = Mouse.GetPosition(mainWindow);
            bool isOverDockZone = mousePos.Y >= -20 && mousePos.Y <= 50 &&
                                  mousePos.X >= -20 && mousePos.X <= mainWindow.ActualWidth + 20;

            if (isOverDockZone)
            {
                if (_currentAdorner == null)
                {
                    // NEW: Ask WPF to find the AdornerLayer above the ROOT ELEMENT, not the window
                    var adornerLayer = AdornerLayer.GetAdornerLayer(rootElement);
                    if (adornerLayer != null)
                    {
                        double barHeight = this.ActualHeight > 0 ? this.ActualHeight : 32;
                        Rect dockRect = new Rect(0, 0, mainWindow.ActualWidth, barHeight);

                        // NEW: We adorn the root element
                        _currentAdorner = new DockingGhostAdorner(rootElement, dockRect);
                        adornerLayer.Add(_currentAdorner);
                    }
                }
            }
            else
            {
                ClearGhostAdorner();
            }
        }

        public void ClearGhostAdorner()
        {
            if (_currentAdorner != null)
            {
                var mainWindow = Window.GetWindow(this);
                // NEW: Ensure we clear it from the correct layer
                if (mainWindow?.Content is UIElement rootElement)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(rootElement);
                    adornerLayer?.Remove(_currentAdorner);
                }
                _currentAdorner = null;
            }
        }
    }
}