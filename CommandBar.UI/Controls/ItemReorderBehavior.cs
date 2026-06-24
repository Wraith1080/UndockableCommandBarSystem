using CommandBar.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CommandBar.UI.Controls
{
    public static class ItemReorderBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ItemReorderBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToolBar toolBar)
            {
                if ((bool)e.NewValue)
                {
                    toolBar.PreviewMouseLeftButtonDown += ToolBar_PreviewMouseLeftButtonDown;
                    toolBar.PreviewMouseMove += ToolBar_PreviewMouseMove;
                    toolBar.Drop += ToolBar_Drop;
                    toolBar.DragOver += ToolBar_DragOver;
                    toolBar.AllowDrop = true;
                }
                else
                {
                    toolBar.PreviewMouseLeftButtonDown -= ToolBar_PreviewMouseLeftButtonDown;
                    toolBar.PreviewMouseMove -= ToolBar_PreviewMouseMove;
                    toolBar.Drop -= ToolBar_Drop;
                    toolBar.DragOver -= ToolBar_DragOver;
                    toolBar.AllowDrop = false;
                }
            }
        }

        private static Point _dragStartPoint;
        private static FrameworkElement? _draggedItemContainer;
        private static CommandItem? _draggedItemData;

        private static void ToolBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ONLY ALLOW DRAGGING IF THE ALT KEY IS HELD DOWN!
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                var toolBar = (ToolBar)sender;
                _dragStartPoint = e.GetPosition(toolBar);

                // Find out exactly which button the mouse is hovering over
                var hit = VisualTreeHelper.HitTest(toolBar, _dragStartPoint);
                if (hit != null)
                {
                    var container = FindAncestor<FrameworkElement>(hit.VisualHit, c => c.DataContext is CommandItem);
                    if (container != null)
                    {
                        _draggedItemContainer = container;
                        _draggedItemData = container.DataContext as CommandItem;
                        e.Handled = true; // Stop the button from actually "clicking"
                    }
                }
            }
        }

        private static void ToolBar_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedItemContainer != null && _draggedItemData != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition((ToolBar)sender);

                // Only trigger the drag if they moved the mouse a tiny bit (prevents accidental micro-drags)
                if (Math.Abs(currentPos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DataObject data = new DataObject("CommandItemFormat", _draggedItemData);
                    DragDrop.DoDragDrop(_draggedItemContainer, data, DragDropEffects.Move);

                    _draggedItemContainer = null;
                    _draggedItemData = null;
                }
            }
        }

        private static void ToolBar_DragOver(object sender, DragEventArgs e)
        {
            // Only allow dropping if the payload is a CommandItem
            if (!e.Data.GetDataPresent("CommandItemFormat"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private static void ToolBar_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("CommandItemFormat") && sender is ToolBar toolBar)
            {
                var droppedData = e.Data.GetData("CommandItemFormat") as CommandItem;
                var model = toolBar.DataContext as ToolbarModel;

                if (droppedData == null || model == null) return;

                // 1. Where did the mouse let go?
                Point dropPoint = e.GetPosition(toolBar);
                var hit = VisualTreeHelper.HitTest(toolBar, dropPoint);

                int insertIndex = model.DockedItems.Count; // Default to pushing it to the very end

                // 2. Did they drop it on top of another button?
                if (hit != null)
                {
                    var targetContainer = FindAncestor<FrameworkElement>(hit.VisualHit, c => c.DataContext is CommandItem);
                    if (targetContainer != null)
                    {
                        var targetData = targetContainer.DataContext as CommandItem;
                        int targetIndex = model.DockedItems.IndexOf(targetData);

                        // Math: Did they drop it on the left half or right half of the target button?
                        Point posInTarget = e.GetPosition(targetContainer);
                        if (toolBar.Orientation == Orientation.Horizontal)
                        {
                            if (posInTarget.X > targetContainer.ActualWidth / 2) targetIndex++;
                        }
                        else
                        {
                            if (posInTarget.Y > targetContainer.ActualHeight / 2) targetIndex++;
                        }
                        insertIndex = targetIndex;
                    }
                }

                // 3. Move the data in the underlying ObservableCollection!
                int oldIndex = model.DockedItems.IndexOf(droppedData);

                if (oldIndex >= 0)
                {
                    // The item is already on this toolbar, we are just reordering it
                    if (oldIndex != insertIndex && oldIndex != insertIndex - 1)
                    {
                        if (oldIndex < insertIndex) insertIndex--; // Shift index back if we are moving forward
                        model.DockedItems.Move(oldIndex, insertIndex);
                    }
                }
                else
                {
                    // 🟢 FIX: The item is brand new from the Customize Dialog (oldIndex == -1). Add it!
                    model.DockedItems.Insert(insertIndex, droppedData);
                }
            }
        }

        // Helper method to climb the visual tree
        private static T? FindAncestor<T>(DependencyObject current, Func<T, bool> predicate) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T tObj && predicate(tObj)) return tObj;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}