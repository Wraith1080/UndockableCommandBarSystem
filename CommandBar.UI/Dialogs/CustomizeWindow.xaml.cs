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