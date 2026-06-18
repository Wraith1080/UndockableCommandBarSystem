using CommandBar.Core.Models;
using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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

        // NEW: Controls how close the mouse must be to an edge(in pixels) to trigger docking
        public double EdgeSnapThreshold { get; set; } = 60.0;

        // NEW: Controls how far the user must drag outside the tray to trigger a tear-off
        public double TearOffThreshold { get; set; } = 30.0;

        public bool IsCustomizeMode { get; set; } = false;

        private UIElement? _dragGrip;
        private bool _isDragging;
        private Point _startMousePosition;
        private Point _itemDragStart;
        private bool _isPreparingToDragItem;
        private InsertionCaretAdorner? _caretAdorner;
        // NEW: Stores the ghost so we can remove it or update its rotation safely!
        private DockingGhostAdorner? _ghostAdorner;

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
            DependencyProperty.Register(nameof(TearOffOffsetX), typeof(double), typeof(UndockableToolBar), new PropertyMetadata(20.0));

        public static readonly DependencyProperty TearOffOffsetYProperty =
            DependencyProperty.Register(nameof(TearOffOffsetY), typeof(double), typeof(UndockableToolBar), new PropertyMetadata(15.0));

        // NEW: Dependency Property so XAML Triggers can see if this is a MenuBar
        public static readonly DependencyProperty IsMenuBarProperty =
            DependencyProperty.Register("IsMenuBar", typeof(bool), typeof(UndockableToolBar), new PropertyMetadata(false));

        public bool IsMenuBar
        {
            get { return (bool)GetValue(IsMenuBarProperty); }
            set { SetValue(IsMenuBarProperty, value); }
        }
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

            // Find the native dotted grip built into the standard WPF ToolBar
            if (GetTemplateChild("ToolBarThumb") is System.Windows.Controls.Primitives.Thumb thumb)
            {
                // Listen to it move instead of hijacking the initial click!
                thumb.DragDelta += Thumb_DragDelta;
            }
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);

            // 1. ARE WE ALREADY FLOATING?
            if (parentWindow is FloatingToolBarWindow floatingWindow)
            {
                // Force the native grip to release the mouse...
                var thumb = sender as System.Windows.Controls.Primitives.Thumb;
                thumb?.CancelDrag();

                // ...and instantly pass control to your custom floating window drag loop!
                Point offset = Mouse.GetPosition(floatingWindow);
                floatingWindow.StartManualDrag(offset.X, offset.Y);
                return;
            }

            // 2. WE ARE DOCKED IN THE TRAY
            var tray = this.Parent as ToolBarTray ?? System.Windows.Media.VisualTreeHelper.GetParent(this) as ToolBarTray;

            if (tray != null)
            {
                Point mousePos = Mouse.GetPosition(tray);
                double tearOffThreshold = TearOffThreshold;

                // 3. Did they drag it off the tray?
                if (mousePos.X < -tearOffThreshold || mousePos.Y < -tearOffThreshold ||
                    mousePos.X > tray.ActualWidth + tearOffThreshold || mousePos.Y > tray.ActualHeight + tearOffThreshold)
                {
                    // THE SEAMLESS HANDOFF
                    var thumb = sender as System.Windows.Controls.Primitives.Thumb;
                    thumb?.CancelDrag();

                    InitiateTearOff();
                }
            }
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

            var floatingBar = new UndockableToolBar();
            floatingBar.DataContext = this.DataContext;
            floatingBar.IsMenuBar = this.IsMenuBar;

            // 2. THE FIX: Reconstruct the visuals properly!
            if (this.IsMenuBar)
            {
                // Inject the native menu into the floating window
                var nativeMenu = new Menu { Background = System.Windows.Media.Brushes.Transparent };
                nativeMenu.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DockedItems") { Source = this.DataContext });
                nativeMenu.SetResourceReference(ItemsControl.ItemContainerStyleProperty, "NativeMenuBarItemStyle");

                floatingBar.Items.Add(nativeMenu);
            }
            else
            {
                // Standard toolbar data binding
                floatingBar.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DockedItems") { Source = this.DataContext });
            }

            // 3. Strip focus scope so it doesn't steal window activation! (From our earlier fix)
            System.Windows.Input.FocusManager.SetIsFocusScope(floatingBar, false);

            floatingWindow.Content = floatingBar;

            if (this.DataContext is ToolbarModel model)
            {
                model.RequestDockChange(DockLocation.Floating);
            }

            // 2. Get the exact screen coordinates of the cursor
            GetCursorPos(out POINT p); // (Requires the Win32 GetCursorPos import you used in FloatingToolBarWindow)

            // 3. Center the new floating window on the mouse
            floatingWindow.Left = p.X - TearOffOffsetX;
            floatingWindow.Top = p.Y - TearOffOffsetY;

            // 4. Show the window
            floatingWindow.Show();

            // 5. Instantly resume the custom math drag so the user never notices the transition!
            floatingWindow.StartManualDrag(TearOffOffsetX, TearOffOffsetY);
        }

        public void CheckForRedock(FloatingToolBarWindow floatingWindow)
        {
            ClearGhostAdorner();

            var mainWindow = Application.Current.MainWindow;
            // THE FIX: Change UIElement to FrameworkElement
            if (mainWindow == null || mainWindow.Content is not FrameworkElement mainContent) return;

            Point mousePos = Mouse.GetPosition(mainContent);
            DockLocation targetDock = CalculateDockZone(mousePos, mainContent);

            if (targetDock != DockLocation.Floating)
            {
                if (this.DataContext is ToolbarModel model)
                {
                    // Because DataContext is now set, this will successfully fire!
                    model.RequestDockChange(targetDock);
                }

                floatingWindow.Close();
            }
        }

        public void UpdateGhostAdorner(FloatingToolBarWindow floatingWindow)
        {
            var mainWindow = Application.Current.MainWindow;
            // FIX: Target the Content of the window, not the Window root!
            // THE FIX: Change UIElement to FrameworkElement
            if (mainWindow == null || mainWindow.Content is not FrameworkElement mainContent) return;

            Point mousePos = Mouse.GetPosition(mainContent);
            DockLocation targetDock = CalculateDockZone(mousePos, mainContent);

            if (targetDock != DockLocation.Floating)
            {
                if (_ghostAdorner == null)
                {
                    // Now that we are starting at the Content, WPF will successfully find the Adorner Layer!
                    var layer = AdornerLayer.GetAdornerLayer(mainContent);
                    if (layer != null)
                    {
                        // Attach the ghost to the Content frame
                        _ghostAdorner = new DockingGhostAdorner(mainContent);
                        layer.Add(_ghostAdorner);
                    }
                }

                _ghostAdorner?.UpdateTargetDock(targetDock);
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
            if (_ghostAdorner != null)
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow?.Content is UIElement mainContent)
                {
                    var layer = AdornerLayer.GetAdornerLayer(mainContent);
                    layer?.Remove(_ghostAdorner);
                }
                _ghostAdorner = null;
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsCustomizeMode)
            {
                base.OnPreviewMouseLeftButtonDown(e);
                return;
            }

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
            if (!IsCustomizeMode)
            {
                base.OnPreviewMouseMove(e);
                return;
            }
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
            if (VisualTreeHelper.HitTest(this, startPoint)?.VisualHit is not DependencyObject hit) return;

            var container = ItemsControl.ContainerFromElement(this, hit) as ContentPresenter;
            if (container == null) return;

            var dataItem = this.ItemContainerGenerator.ItemFromContainer(container);
            if (dataItem == null) return;

            // NEW: Package BOTH the item and its source collection into the drag payload
            var dataObj = new DataObject();
            dataObj.SetData("CommandItemFormat", dataItem);
            dataObj.SetData("SourceListFormat", this.ItemsSource); // Pass the original list!

            DragDrop.DoDragDrop(container, dataObj, DragDropEffects.Move);

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

            // Extract the item AND the source list from the payload
            var draggedData = e.Data.GetData("CommandItemFormat") as CommandItem;
            var sourceList = e.Data.GetData("SourceListFormat") as System.Collections.IList;

            if (draggedData == null || this.ItemsSource is not System.Collections.IList targetList) return;

            int insertIndex = CalculateInsertionState(e.GetPosition(this), out _);

            // NEW: Decide if this is an internal shuffle or a cross-toolbar steal
            if (sourceList == targetList)
            {
                // It's the same toolbar. Just shuffle the index.
                int oldIndex = targetList.IndexOf(draggedData);
                if (oldIndex != -1 && oldIndex != insertIndex)
                {
                    if (oldIndex < insertIndex) insertIndex--;
                    targetList.RemoveAt(oldIndex);
                    targetList.Insert(insertIndex, draggedData);
                }
            }
            else
            {
                // It's a DIFFERENT toolbar! Steal it from the source list.
                sourceList?.Remove(draggedData);
                targetList.Insert(insertIndex, draggedData);
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
        // NEW: Calculates which zone the mouse is currently hovering over
        private DockLocation CalculateDockZone(Point mousePos, FrameworkElement dockContainer)
        {
            // USE THE PROPERTY instead of the magic number
            double edgeThickness = EdgeSnapThreshold;

            if (mousePos.Y < edgeThickness) return DockLocation.Top;
            if (mousePos.Y > dockContainer.ActualHeight - edgeThickness) return DockLocation.Bottom;
            if (mousePos.X < edgeThickness) return DockLocation.Left;
            if (mousePos.X > dockContainer.ActualWidth - edgeThickness) return DockLocation.Right;

            return DockLocation.Floating;
        }
    }
}