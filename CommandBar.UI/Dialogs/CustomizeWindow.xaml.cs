using CommandBar.Core.Models;
using System;
using System.Text.RegularExpressions;
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

        // --- INPUT OVERLAY LOGIC ---
        private Action<string>? _inputCallback;

        private void ShowInput(string prompt, string defaultText, Action<string> callback)
        {
            InputPromptText.Text = prompt;
            InputTextBox.Text = defaultText;
            _inputCallback = callback;
            InputOverlay.Visibility = Visibility.Visible;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void InputOk_Click(object sender, RoutedEventArgs e)
        {
            InputOverlay.Visibility = Visibility.Collapsed;
            _inputCallback?.Invoke(InputTextBox.Text.Trim());
        }

        private void InputCancel_Click(object sender, RoutedEventArgs e)
        {
            InputOverlay.Visibility = Visibility.Collapsed;
        }

        // --- BUTTON LOGIC ---
        private void NewToolbar_Click(object sender, RoutedEventArgs e)
        {
            ShowInput("Enter new toolbar name:", "", (name) =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                if (this.DataContext is CommandBarManager manager)
                {
                    // Pass true for IsCustom!
                    var newTb = manager.CreateToolbar(name, DockLocation.Floating, 0, 0, false, true);
                    newTb.FloatingLeft = this.Left + 50;
                    newTb.FloatingTop = this.Top + 50;
                    manager.RestoreFloatingWindowAction?.Invoke(newTb);
                }
            });
        }

        private void RenameToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (ToolbarListBox.SelectedItem is ToolbarModel tb)
            {
                if (!tb.IsCustom)
                {
                    MessageBox.Show("You cannot rename default toolbars.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                ShowInput("Rename toolbar:", tb.Name, (newName) =>
                {
                    if (!string.IsNullOrWhiteSpace(newName)) tb.Name = newName;
                });
            }
        }

        private void DeleteToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (ToolbarListBox.SelectedItem is ToolbarModel tb)
            {
                if (!tb.IsCustom)
                {
                    MessageBox.Show("You cannot delete default toolbars.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (this.DataContext is CommandBarManager manager)
                {
                    manager.DeleteToolbar(tb);
                }
            }
        }

        private void ResetToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (ToolbarListBox.SelectedItem is ToolbarModel tb)
            {
                if (tb.IsCustom)
                {
                    MessageBox.Show("Custom toolbars do not have a factory default state.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (this.DataContext is CommandBarManager manager && System.IO.File.Exists("DefaultLayout.json"))
                {
                    manager.ResetToolbar(tb, System.IO.File.ReadAllText("DefaultLayout.json"));
                }
            }
        }

        // Add this to your usings at the top if it isn't there:
        // using System.Text.RegularExpressions;

        private void BrowseSvg_Click(object sender, RoutedEventArgs e)
        {
            if (MasterCommandList.SelectedItem is CommandItem selectedCmd)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select an SVG Icon",
                    Filter = "SVG Files (*.svg)|*.svg|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string svgContent = System.IO.File.ReadAllText(dialog.FileName);

                        // Extract the vector data from the d="..." attribute
                        var match = Regex.Match(svgContent, @"d=""([^""]+)""");
                        if (match.Success)
                        {
                            selectedCmd.IconGeometry = match.Groups[1].Value;
                        }
                        else
                        {
                            MessageBox.Show("Could not find a valid vector path (d=\"...\") in this SVG file.", "Invalid SVG", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading SVG file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ResetCommand_Click(object sender, RoutedEventArgs e)
        {
            if (MasterCommandList.SelectedItem is CommandItem selectedCmd)
            {
                // Restoring these fires the PropertyChanged event automatically!
                selectedCmd.Text = selectedCmd.DefaultText;
                selectedCmd.Tooltip = selectedCmd.DefaultTooltip;
                selectedCmd.IconGeometry = selectedCmd.DefaultIconGeometry;
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