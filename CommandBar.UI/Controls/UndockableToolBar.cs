using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections;
using System.Windows.Media;
using CommandBar.Core.Models;

namespace CommandBar.UI.Controls
{
    public class UndockableToolBar : ToolBar
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private UIElement? _dragGrip;
        private bool _isDragging;
        private Point _startMousePosition;
        private Point _itemDragStart;
        private bool _isPreparingToDragItem;
        private InsertionCaretAdorner? _caretAdorner;

        public UndockableToolBar()
        {
            // CRITICAL: Tells the OS this control is allowed to accept dropped items
            AllowDrop = true;
            // NEW: Prevent the toolbar background and drag-grip from accepting focus
            Focusable = false;
        }

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

                    // REPLACED WPF DRAG
                    floatingWindow.NoActivateDragMove();

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

            // NEW: Bind the floating palette to the main application window!
            // This prevents the OS from treating it as a competing, separate application.
            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                floatingWindow.Owner = mainWindow;
            }

            var floatingBar = new UndockableToolBar
            {
                ItemsSource = this.ItemsSource
            };

            // CHANGED: Rip out the C# Border. Just assign the bar.
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
                // NEW: Start the silent, mathematical WPF drag loop! 
                // This bypasses Win32 entirely, guaranteeing zero focus theft.
                floatingWindow.StartInitialManualDrag(TearOffOffsetX, TearOffOffsetY);
            }
            else
            {
                // Fallback in case they release the mouse instantly
                CheckForRedock(floatingWindow);
            }
        }

        public void CheckForRedock(FloatingToolBarWindow floatingWindow)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow == null) return;

            // NEW: Get physical OS mouse pixels and translate them to WPF logical window pixels
            GetCursorPos(out POINT p);
            Point mousePos = mainWindow.PointFromScreen(new Point(p.X, p.Y));

            bool isOverDockZone = mousePos.Y >= -20 && mousePos.Y <= 50 &&
                                  mousePos.X >= -20 && mousePos.X <= mainWindow.ActualWidth + 20;

            if (isOverDockZone)
            {
                floatingWindow.Close();
                this.Visibility = Visibility.Visible;
            }
        }

        public void UpdateGhostAdorner(FloatingToolBarWindow floatingWindow)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow == null || mainWindow.Content is not UIElement rootElement) return;

            // NEW: Get physical OS mouse pixels and translate them to WPF logical window pixels
            GetCursorPos(out POINT p);
            Point mousePos = mainWindow.PointFromScreen(new Point(p.X, p.Y));

            bool isOverDockZone = mousePos.Y >= -20 && mousePos.Y <= 50 &&
                                  mousePos.X >= -20 && mousePos.X <= mainWindow.ActualWidth + 20;

            if (isOverDockZone)
            {
                if (_currentAdorner == null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(rootElement);
                    if (adornerLayer != null)
                    {
                        double barHeight = this.ActualHeight > 0 ? this.ActualHeight : 32;
                        Rect dockRect = new Rect(0, 0, mainWindow.ActualWidth, barHeight);

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

        // NEW: Event handler that fires continuously while the floating window is dragged
        private void FloatingWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (sender is FloatingToolBarWindow floatingWindow && floatingWindow.OriginalToolBar != null)
            {
                floatingWindow.OriginalToolBar.UpdateGhostAdorner(floatingWindow);
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

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            // If they clicked the drag grip, ignore it! (Let your existing Tear-off logic handle it)
            if (_dragGrip != null && _dragGrip.IsMouseOver) return;

            // NEW: Use native WPF hit testing to find the clicked visual element
            if (VisualTreeHelper.HitTest(this, e.GetPosition(this))?.VisualHit is DependencyObject hit)
            {
                // Check if the hit element belongs to one of our generated item containers
                var container = ItemsControl.ContainerFromElement(this, hit) as ContentPresenter;
                if (container != null)
                {
                    _isPreparingToDragItem = true;
                    _itemDragStart = e.GetPosition(this);
                }
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (_isPreparingToDragItem && e.LeftButton == MouseButtonState.Pressed)
            {
                Vector diff = e.GetPosition(this) - _itemDragStart;

                // If they dragged the mouse past the "accidental twitch" threshold
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isPreparingToDragItem = false;
                    StartItemDragDrop(e.GetPosition(this));
                }
            }
        }

        private void StartItemDragDrop(Point startPoint)
        {
            // NEW: Find the original clicked element natively
            if (VisualTreeHelper.HitTest(this, startPoint)?.VisualHit is not DependencyObject hit) return;

            var container = ItemsControl.ContainerFromElement(this, hit) as ContentPresenter;
            if (container == null) return;

            var dataItem = this.ItemContainerGenerator.ItemFromContainer(container);
            if (dataItem == null) return;

            DragDrop.DoDragDrop(container, new DataObject("CommandItemFormat", dataItem), DragDropEffects.Move);

            RemoveCaret();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            // Only react if the thing being dragged is our custom command format
            if (!e.Data.GetDataPresent("CommandItemFormat"))
            {
                e.Effects = DragDropEffects.None;
                RemoveCaret();
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            DrawCaret(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);

            // Calculate if the mouse has physically left the boundaries of the toolbar.
            // If it's still inside, it means we are just moving between internal buttons!
            Point pos = e.GetPosition(this);
            if (pos.X < 0 || pos.X >= this.ActualWidth || pos.Y < 0 || pos.Y >= this.ActualHeight)
            {
                RemoveCaret();
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            RemoveCaret();

            var draggedData = e.Data.GetData("CommandItemFormat") as CommandItem;
            if (draggedData == null || this.ItemsSource is not IList list) return;

            // NEW: Use the math engine to find the drop index
            int insertIndex = CalculateInsertionState(e.GetPosition(this), out _);
            int oldIndex = list.IndexOf(draggedData);

            if (oldIndex != -1 && oldIndex != insertIndex)
            {
                if (oldIndex < insertIndex) insertIndex--;

                list.RemoveAt(oldIndex);
                list.Insert(insertIndex, draggedData);
            }
            else if (oldIndex == -1)
            {
                list.Insert(insertIndex, draggedData);
            }

            e.Handled = true;
        }

        // --- VISUAL & MATH HELPERS ---

        // --- NEW GEOMETRIC MATH ENGINE ---

        private void DrawCaret(Point mousePos)
        {
            if (_caretAdorner == null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer == null) return;
                _caretAdorner = new InsertionCaretAdorner(this);
                adornerLayer.Add(_caretAdorner);
            }

            CalculateInsertionState(mousePos, out double caretX);
            _caretAdorner.UpdatePosition(new Point(caretX, 0));
        }

        private int CalculateInsertionState(Point mousePos, out double caretX)
        {
            int targetIndex = this.Items.Count;
            caretX = 0;

            if (this.Items.Count == 0) return 0;

            UIElement? lastContainer = null;

            // Iterate through every visible item in the toolbar
            for (int i = 0; i < this.Items.Count; i++)
            {
                var container = this.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                if (container == null) continue;

                lastContainer = container;

                // Calculate the exact mathematical midpoint of the button
                Point containerPos = container.TranslatePoint(new Point(0, 0), this);
                double containerMidpoint = containerPos.X + (container.RenderSize.Width / 2);

                // If the mouse is to the left of the midpoint, this is our drop target!
                if (mousePos.X < containerMidpoint)
                {
                    targetIndex = i;
                    caretX = containerPos.X; // Snap caret to the left edge
                    return targetIndex;
                }
            }

            // If we looped through everything, the mouse is past the very last button
            if (lastContainer != null)
            {
                Point lastPos = lastContainer.TranslatePoint(new Point(0, 0), this);
                caretX = lastPos.X + lastContainer.RenderSize.Width; // Snap caret to the right edge
            }

            return targetIndex;
        }

        private void RemoveCaret()
        {
            if (_caretAdorner != null)
            {
                AdornerLayer.GetAdornerLayer(this)?.Remove(_caretAdorner);
                _caretAdorner = null;
            }
        }
    }
}