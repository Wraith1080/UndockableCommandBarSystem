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

        private Point _separatorDragStart;

        public CustomizeWindow()
        {
            InitializeComponent();
            // 1. Read the current size from the application resources (default to 16 if it somehow fails)
            if (Application.Current.Resources["CmdBar.IconSize"] is double currentSize)
            {
                IconScaleSlider.Value = currentSize;
            }
            else
            {
                IconScaleSlider.Value = 16;
            }

            // 2. NOW subscribe to the event so it only fires when the user actually drags it!
            IconScaleSlider.ValueChanged += IconScaleSlider_ValueChanged;
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

                        // 1. Save the raw file for SharpVectors
                        selectedCmd.RawSvgContent = svgContent;

                        // 2. Keep the F1 extraction for the Monochrome fallback
                        var matches = Regex.Matches(svgContent, @"d=(?:""|')([^""']+)(?:""|')");
                        if (matches.Count > 0)
                        {
                            var allPaths = new System.Collections.Generic.List<string>();
                            foreach (Match m in matches) allPaths.Add(m.Groups[1].Value);
                            selectedCmd.IconGeometry = "F1 " + string.Join(" ", allPaths);
                        }

                        // 3. Auto-check the toggle for them!
                        selectedCmd.KeepOriginalColors = true;
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
                selectedCmd.RawSvgContent = selectedCmd.DefaultRawSvgContent;
                selectedCmd.KeepOriginalColors = selectedCmd.DefaultKeepOriginalColors;
            }
        }

        // --- MENU BAR TREEVIEW EDITOR LOGIC ---

        private void AddMenuRoot_Click(object sender, RoutedEventArgs e)
        {
            ShowInput("Enter Root Menu Name (e.g. 'Tools'):", "", (name) =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                if (this.DataContext is CommandBarManager manager && manager.MainMenuBar != null)
                {
                    // Create a brand new Dropdown Item and add it to the physical Menu Bar!
                    var newMenu = new CommandDropdownItem { Id = Guid.NewGuid().ToString(), Text = name };
                    manager.MainMenuBar.DockedItems.Add(newMenu);
                }
            });
        }

        private void AddSubMenu_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandDropdownItem parentMenu)
            {
                ShowInput("Enter Sub-Menu Name:", "", (name) =>
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var subMenu = new CommandDropdownItem { Id = Guid.NewGuid().ToString(), Text = name };
                        
                        parentMenu.ChildItems.Add(subMenu);
                    }
                });
            }
            else
            {
                MessageBox.Show("Please select a Folder (📁) in the tree first.", "Cannot Add Folder", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InsertMenuCommand_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandDropdownItem parentMenu && MenuCommandPicker.SelectedItem is CommandItem cmdToCopy)
            {
                // Clone the selected command so it doesn't rip it out of the Master Dictionary
                var clone = cmdToCopy.Clone();
                
                parentMenu.ChildItems.Add(clone);
            }
            else
            {
                MessageBox.Show("Please select a Folder (📁) in the tree, and pick a Command from the dropdown.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandItem selectedNode)
            {
                if (this.DataContext is CommandBarManager manager && manager.MainMenuBar != null)
                {
                    // Is it a root item?
                    if (manager.MainMenuBar.DockedItems.Contains(selectedNode))
                    {
                        manager.MainMenuBar.DockedItems.Remove(selectedNode);
                        return;
                    }

                    // Otherwise, we have to recursively search the tree to find its parent to remove it
                    RemoveNodeRecursive(manager.MainMenuBar.DockedItems, selectedNode);
                }
            }
        }

        // --- 🟢 NEW HELPER: Find an item's parent collection so we can move it! ---
        private System.Collections.ObjectModel.ObservableCollection<CommandItem>? FindParentCollection(System.Collections.ObjectModel.ObservableCollection<CommandItem> root, CommandItem target)
        {
            if (root.Contains(target)) return root;
            foreach (var item in root.OfType<CommandDropdownItem>())
            {
                var found = FindParentCollection(item.ChildItems, target);
                if (found != null) return found;
            }
            return null;
        }

        // --- 🟢 NEW: MOVE UP & DOWN ---
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandItem selected && this.DataContext is CommandBarManager manager && manager.MainMenuBar != null)
            {
                var collection = FindParentCollection(manager.MainMenuBar.DockedItems, selected);
                if (collection != null)
                {
                    int index = collection.IndexOf(selected);
                    // ObservableCollection.Move() instantly updates the TreeView AND the physical Menu UI!
                    if (index > 0) collection.Move(index, index - 1);
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandItem selected && this.DataContext is CommandBarManager manager && manager.MainMenuBar != null)
            {
                var collection = FindParentCollection(manager.MainMenuBar.DockedItems, selected);
                if (collection != null)
                {
                    int index = collection.IndexOf(selected);
                    if (index >= 0 && index < collection.Count - 1) collection.Move(index, index + 1);
                }
            }
        }

        // --- 🟢 NEW: INSERT SEPARATOR ---
        private void InsertSeparator_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandDropdownItem parentMenu)
            {
                // A Separator is just a CommandItem where IsSeparator = true
                parentMenu.ChildItems.Add(new CommandSeparator { Id = "SEP_" + Guid.NewGuid().ToString() });
            }
            else
            {
                MessageBox.Show("Please select a Folder (📁) to insert a separator into.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- 🟢 NEW: RESET ROOT MENU ---
        private void ResetMenu_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTreeView.SelectedItem is CommandDropdownItem selectedMenu && this.DataContext is CommandBarManager manager)
            {
                // Fetch the original factory state of this exact menu from the Master Registry
                var masterMenu = manager.GetCommand(selectedMenu.Id) as CommandDropdownItem;

                // Make sure it is an original Root menu (like "File"), and not a user-created custom folder
                if (masterMenu != null && manager.MainMenuBar != null && manager.MainMenuBar.DockedItems.Contains(selectedMenu))
                {
                    selectedMenu.ChildItems.Clear();
                    foreach (var child in masterMenu.ChildItems)
                    {
                        selectedMenu.ChildItems.Add(child.Clone());
                    }
                }
                else
                {
                    MessageBox.Show("Only default Root Menus (like 'File' or 'Edit') can be factory reset.", "Cannot Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private bool RemoveNodeRecursive(IEnumerable<CommandItem> items, CommandItem target)
        {
            foreach (var item in items.OfType<CommandDropdownItem>())
            {
                if (item.ChildItems != null)
                {
                    if (item.ChildItems.Contains(target))
                    {
                        item.ChildItems.Remove(target);
                        return true;
                    }
                    if (RemoveNodeRecursive(item.ChildItems, target)) return true;
                }
            }
            return false;
        }

        private void DraggableSeparatorTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Record the exact pixel where the user clicked the border
            _separatorDragStart = e.GetPosition(null);
        }

        private void DraggableSeparatorTool_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _separatorDragStart - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Change this line:
                    var dropSeparator = new CommandSeparator { Id = "SEP_" + Guid.NewGuid().ToString() };

                    // 🟢 FIX: Wrap it in the DataObject using the exact string key the Toolbar expects!
                    DataObject dragData = new DataObject("CommandItemFormat", dropSeparator);

                    DragDrop.DoDragDrop(DraggableSeparatorTool, dragData, DragDropEffects.Copy);
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

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Prevent the theme from swapping while the window is still constructing itself
            if (!IsLoaded) return;

            if (ThemeModernRadio?.IsChecked == true)
            {
                CommandBar.UI.ThemeManager.ChangeTheme("Modern");
            }
            else if (ThemeClassicRadio?.IsChecked == true)
            {
                CommandBar.UI.ThemeManager.ChangeTheme("Classic");
            }
        }

        // Allows the user to drag the custom window around the screen
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // Closes the dialog
        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void IconScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Application.Current == null) return;

            // 1. Update the Live UI instantly
            Application.Current.Resources["CmdBar.IconSize"] = e.NewValue;

            // 2. Update the Manager so it saves the correct value to JSON!
            // (Ensure you reference your local CommandBarManager instance here)

            if (this.DataContext is CommandBarManager manager)
            {
                manager.CurrentIconSize = e.NewValue;
            }
        }
    }
}