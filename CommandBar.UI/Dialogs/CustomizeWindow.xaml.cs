using CommandBar.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CommandBar.UI.Dialogs
{
    public partial class CustomizeWindow : Window
    {
        private Point _dragStartPoint;

        public CustomizeWindow()
        {
            InitializeComponent();
        }

        private void MasterCommandList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void MasterCommandList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listViewItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                    if (listViewItem != null && listViewItem.DataContext is CommandItem commandToDrag)
                    {
                        // Clone the item so we don't drain the master pool!
                        DataObject dragData = new DataObject("CommandItemFormat", commandToDrag.Clone());
                        DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);

                    }
                }
            }
        }

        private void CreateNewToolbar_Click(object sender, RoutedEventArgs e)
        {
            string name = NewToolbarNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            // Because we set the DataContext to the Manager when opening this window, we can safely cast it!
            if (this.DataContext is CommandBarManager manager)
            {
                // 1. Create a blank toolbar, defaulted to the Floating state
                var newToolbar = manager.CreateToolbar(name, DockLocation.Floating, 0, 0, false);

                // 2. Set its initial floating coordinates to be slightly offset from this dialog window
                newToolbar.FloatingLeft = this.Left + 50;
                newToolbar.FloatingTop = this.Top + 50;

                // 3. Trigger the UI Action we built earlier to instantly summon the floating window!
                manager.RestoreFloatingWindowAction?.Invoke(newToolbar);

                // 4. Clean up the UI
                NewToolbarNameBox.Clear();

                // Optional: Swap the user immediately over to the Commands tab so they can start dragging!
                if (NewToolbarNameBox.Parent is FrameworkElement parent &&
                    VisualTreeHelper.GetParent(parent) is Grid grid &&
                    VisualTreeHelper.GetParent(grid) is TabItem tabItem &&
                    tabItem.Parent is TabControl tabControl)
                {
                    tabControl.SelectedIndex = 1;
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T tObj) return tObj;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}