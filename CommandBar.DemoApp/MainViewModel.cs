using CommandBar.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace CommandBar.DemoApp
{
    public partial class MainViewModel : ObservableObject
    {
        public CommandBarManager Manager { get; } = new();

        // Instead of a single Toolbar, the UI will bind to the Manager's list of active toolbars
        public ObservableCollection<ToolbarModel> TopToolbars => Manager.TopToolbars;
        public ObservableCollection<ToolbarModel> BottomToolbars => Manager.BottomToolbars;
        public ObservableCollection<ToolbarModel> LeftToolbars => Manager.LeftToolbars;
        public ObservableCollection<ToolbarModel> RightToolbars => Manager.RightToolbars;

        private Window? _customizeDialogInstance;

        // Command to reset a specific toolbar
        public RelayCommand<ToolbarModel> ResetToolbarCommand { get; }

        // Command to open the dialog
        public RelayCommand OpenCustomizeDialogCommand { get; }

        public MainViewModel()
        {
            Manager = new CommandBarManager();
            RegisterAllAppCommands();

            // THE FIX: We bridge the gap here! The App tells the Core how to open the UI.
            Manager.OpenCustomizeDialogAction = () =>
            {
                // Prevent multiple copies! If it's already open, just focus it!
                if (_customizeDialogInstance != null)
                {
                    _customizeDialogInstance.Focus();
                    return;
                }

                _customizeDialogInstance = new CommandBar.UI.Dialogs.CustomizeWindow();
                _customizeDialogInstance.DataContext = this.Manager;

                // THE FIX: Tether the dialog's lifecycle to the Main Window!
                if (Application.Current.MainWindow != null)
                {
                    _customizeDialogInstance.Owner = Application.Current.MainWindow;
                }

                _customizeDialogInstance.Closed += (s, e) =>
                {
                    Manager.IsCustomizeMode = false;
                    _customizeDialogInstance = null; // Clear the instance when they close it
                };

                _customizeDialogInstance.Show();
            };

            // 🟢 THE FIX: Teach the application how to instantly recreate a floating window on launch!
            Manager.RestoreFloatingWindowAction = (toolbarModel) =>
            {
                var floatingWindow = new CommandBar.UI.Controls.FloatingToolBarWindow();
                var floatingBar = new CommandBar.UI.Controls.UndockableToolBar();

                floatingBar.DataContext = toolbarModel;
                floatingBar.IsMenuBar = toolbarModel.IsMenuBar;
                floatingWindow.OriginalToolBar = floatingBar; // Self-reference so redocking works!

                if (Application.Current.MainWindow != null)
                {
                    floatingWindow.Owner = Application.Current.MainWindow;
                    floatingBar.SetBinding(CommandBar.UI.Controls.UndockableToolBar.IsCustomizeModeProperty,
                        new System.Windows.Data.Binding("DataContext.Manager.IsCustomizeMode") { Source = Application.Current.MainWindow });
                }

                // Reconstruct the internal menu/items structure
                if (toolbarModel.IsMenuBar)
                {
                    var nativeMenu = new System.Windows.Controls.Menu { Background = System.Windows.Media.Brushes.Transparent };
                    nativeMenu.SetBinding(System.Windows.Controls.ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("DockedItems") { Source = toolbarModel });
                    nativeMenu.SetResourceReference(System.Windows.Controls.ItemsControl.ItemContainerStyleProperty, "NativeMenuBarItemStyle");
                    floatingBar.Items.Add(nativeMenu);
                }
                else
                {
                    floatingBar.SetBinding(System.Windows.Controls.ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding("DockedItems") { Source = toolbarModel });
                }

                System.Windows.Input.FocusManager.SetIsFocusScope(floatingBar, false);
                floatingWindow.Content = floatingBar;

                // Restore exact coordinates!
                floatingWindow.Left = toolbarModel.FloatingLeft;
                floatingWindow.Top = toolbarModel.FloatingTop;

                // Hide the floating window if the user unchecks it in the dialog!
                floatingWindow.SetBinding(Window.VisibilityProperty, new Binding("IsVisible")
                {
                    Source = toolbarModel,
                    Converter = new System.Windows.Controls.BooleanToVisibilityConverter()
                });

                floatingWindow.Show();
            };

            // Hook up the button command to trigger the Manager
            OpenCustomizeDialogCommand = new RelayCommand(Manager.ShowCustomizeDialog);

            // Initialize the Reset Command
            ResetToolbarCommand = new RelayCommand<ToolbarModel>(toolbar =>
            {
                if (toolbar != null && File.Exists("DefaultLayout.json"))
                {
                    string json = File.ReadAllText("DefaultLayout.json");
                    Manager.ResetToolbar(toolbar, json);
                }
            });

        }

        private void RegisterAllAppCommands()
        {
            // --- 1. SVG ICON PATHS ---
            string iconNew = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13,9V3.5L18.5,9H13Z";
            string iconOpen = "M19,20H4C2.89,20 2,19.1 2,18V6C2,4.89 2.89,4 4,4H10L12,6H19A2,2 0 0,1 21,8H21L4,8V18L6.14,10.7C6.4,9.88 7.24,9.25 8.28,9.25H19.02L19,20Z";
            string iconSave = "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z";
            string iconSaveAs = "M17,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H11.81C11.42,20.34 11.17,19.6 11.07,18.84C9.5,18.31 8.66,16.6 9.2,15.03C9.61,13.83 10.73,13 12,13C12.44,13 12.88,13.1 13.28,13.29C13.62,12.08 14.83,11.23 16.16,11.23C16.89,11.23 17.57,11.46 18.11,11.85C18.66,10.66 19.85,9.88 21.2,9.88V7L17,3M15,9H5V5H15V9M22.1,16.7C22.3,16.9 22.3,17.2 22.1,17.4L21.1,18.4L18.6,15.9L19.6,14.9C19.8,14.7 20.1,14.7 20.3,14.9L22.1,16.7M14,20.5V23H16.5L20.6,18.9L18.1,16.4L14,20.5Z";

            string iconCut = "M19,3L13,9L15,11L22,4V3M12,12.5A2.5,2.5 0 0,1 9.5,15A2.5,2.5 0 0,1 7,12.5A2.5,2.5 0 0,1 9.5,10A2.5,2.5 0 0,1 12,12.5M6,20A2,2 0 0,1 4,18C4,16.89 4.9,16 6,16A2,2 0 0,1 8,18C8,19.11 7.1,20 6,20M6,8A2,2 0 0,1 4,6C4,4.89 4.9,4 6,4A2,2 0 0,1 8,6C8,7.11 7.1,8 6,8M9.64,7.46C9.87,7.14 10,6.73 10,6.25C10,4.45 8.5,3 6.25,3C4,3 2.5,4.45 2.5,6.25C2.5,8.05 4,9.5 6.25,9.5C6.73,9.5 7.14,9.37 7.46,9.14L11.46,13.14L7.46,17.14C7.14,16.87 6.73,16.75 6.25,16.75C4,16.75 2.5,18.2 2.5,20C2.5,21.8 4,23.25 6.25,23.25C8.5,23.25 10,21.8 10,20C10,19.52 9.87,19.11 9.64,18.79L13.64,14.79L20,21.15V22.15H22V20.15L9.64,7.46Z";
            string iconCopy = "M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z";
            string iconPaste = "M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3Z";

            string iconUndo = "M12.5,8C9.85,8 7.45,9 5.6,10.6L2,7V16H11L7.38,12.38C8.77,11.22 10.54,10.5 12.5,10.5C16.04,10.5 19.05,12.81 20.1,16L22.47,15.22C21.08,11.03 17.15,8 12.5,8Z";
            string iconRedo = "M18.4,10.6C16.55,9 13.65,8 11.5,8C6.85,8 2.92,11.03 1.53,15.22L3.9,16C4.95,12.81 7.96,10.5 11.5,10.5C13.46,10.5 15.23,11.22 16.62,12.38L13,16H22V7L18.4,10.6Z";

            string iconBold = "M13.5,15.5H10V12.5H13.5A1.5,1.5 0 0,1 15,14A1.5,1.5 0 0,1 13.5,15.5M10,6.5H13A1.5,1.5 0 0,1 14.5,8A1.5,1.5 0 0,1 13,9.5H10M15.6,10.79C16.57,10.11 17.25,9 17.25,8C17.25,5.74 15.5,4 13.25,4H7V18H14.04C16.14,18 17.75,16.3 17.75,14.21C17.75,12.69 16.89,11.39 15.6,10.79Z";
            string iconItalic = "M10,4.5V7H12.21L8.79,15H6V17.5H14V15H11.79L15.21,7H18V4.5H10Z";
            string iconUnderline = "M5,21H19V19H5V21M12,17A6,6 0 0,0 18,11V3H15.5V11A3.5,3.5 0 0,1 12,14.5A3.5,3.5 0 0,1 8.5,11V3H6V11A6,6 0 0,0 12,17Z";

            // --- 2. BASE COMMAND REGISTRATION (Configured for Toolbars as ImageOnly) ---

            // Standard
            Manager.RegisterCommand("App.New", new CommandItem { Text = "New", Tooltip = "New File", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconNew });
            Manager.RegisterCommand("App.Open", new CommandItem { Text = "Open", Tooltip = "Open File", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconOpen });
            Manager.RegisterCommand("App.Save", new CommandItem { Text = "Save", Tooltip = "Save", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconSave });
            Manager.RegisterCommand("App.SaveAs", new CommandItem { Text = "Save As", Tooltip = "Save As...", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconSaveAs });

            // Edit
            Manager.RegisterCommand("App.Cut", new CommandItem { Text = "Cut", Tooltip = "Cut", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconCut });
            Manager.RegisterCommand("App.Copy", new CommandItem { Text = "Copy", Tooltip = "Copy", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconCopy });
            Manager.RegisterCommand("App.Undo", new CommandItem { Text = "Undo", Tooltip = "Undo", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconUndo });
            Manager.RegisterCommand("App.Redo", new CommandItem { Text = "Redo", Tooltip = "Redo", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconRedo });

            // The Paste Dropdown
            var pasteDropdown = new CommandDropdownItem { Text = "Paste", Tooltip = "Paste Options", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconPaste };
            // 🟢 BONUS FIX: Give these dynamic children IDs so they save/load perfectly!
            pasteDropdown.ChildItems.Add(new CommandItem { Id = "App.PasteText", Text = "Paste as Text", IconGeometry = iconPaste, DisplayMode = CommandDisplayMode.ImageAndText });
            pasteDropdown.ChildItems.Add(new CommandItem { Id = "App.PasteSpecial", Text = "Paste Special...", IconGeometry = iconPaste, DisplayMode = CommandDisplayMode.ImageAndText });
            Manager.RegisterCommand("App.PasteMenu", pasteDropdown);

            // Formatting
            Manager.RegisterCommand("App.Bold", new CommandToggleItem { Text = "Bold", Tooltip = "Bold", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconBold });
            Manager.RegisterCommand("App.Italic", new CommandToggleItem { Text = "Italic", Tooltip = "Italic", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconItalic });
            Manager.RegisterCommand("App.Underline", new CommandToggleItem { Text = "Underline", Tooltip = "Underline", DisplayMode = CommandDisplayMode.ImageOnly, IconGeometry = iconUnderline });

            Manager.RegisterCommand("App.Separator", new CommandSeparator());


            // --- 3. MENUBAR REGISTRATION (Using .Clone() to format for Dropdowns!) ---

            // FILE MENU
            var fileMenu = new CommandDropdownItem { Text = "File", DisplayMode = CommandDisplayMode.TextOnly };
            AddClonedCommandToMenu(fileMenu, "App.New");
            AddClonedCommandToMenu(fileMenu, "App.Open");
            fileMenu.ChildItems.Add(new CommandSeparator { Id = "SEP_File_1" });
            AddClonedCommandToMenu(fileMenu, "App.Save");
            AddClonedCommandToMenu(fileMenu, "App.SaveAs");
            Manager.RegisterCommand("Menu.File", fileMenu);

            // EDIT MENU
            var editMenu = new CommandDropdownItem { Text = "Edit", DisplayMode = CommandDisplayMode.TextOnly };
            AddClonedCommandToMenu(editMenu, "App.Undo");
            AddClonedCommandToMenu(editMenu, "App.Redo");
            editMenu.ChildItems.Add(new CommandSeparator { Id = "SEP_Edit_1" });
            AddClonedCommandToMenu(editMenu, "App.Cut");
            AddClonedCommandToMenu(editMenu, "App.Copy");
            AddClonedCommandToMenu(editMenu, "App.PasteMenu"); // Injecting a dropdown into a dropdown!
            Manager.RegisterCommand("Menu.Edit", editMenu);

            // FORMAT MENU
            var formatMenu = new CommandDropdownItem { Text = "Format", DisplayMode = CommandDisplayMode.TextOnly };
            AddClonedCommandToMenu(formatMenu, "App.Bold");
            AddClonedCommandToMenu(formatMenu, "App.Italic");
            AddClonedCommandToMenu(formatMenu, "App.Underline");
            Manager.RegisterCommand("Menu.Format", formatMenu);
        }

        // Helper to clone a command and force it to show text so it looks right inside a Menu
        private void AddClonedCommandToMenu(CommandDropdownItem parentMenu, string commandId)
        {
            var cmd = Manager.GetCommand(commandId)?.Clone();
            if (cmd != null)
            {
                cmd.DisplayMode = CommandDisplayMode.ImageAndText;
                parentMenu.ChildItems.Add(cmd);
            }
        }

        // Add this method anywhere in MainViewModel
        public void SaveCurrentLayout()
        {
            // Save Layout
            string layoutJson = Manager.SaveLayoutToJson();
            File.WriteAllText("user_layout.json", layoutJson);

            // 🟢 NEW: Save Command Customizations
            string commandsJson = Manager.SaveCommandsToJson();
            File.WriteAllText("user_commands.json", commandsJson);
        }

        // Add this anywhere in your MainViewModel class
        public void InitializeLayout()
        {
            // 🟢 NEW: Load Command Customizations FIRST
            if (File.Exists("user_commands.json"))
            {
                string cmdJson = File.ReadAllText("user_commands.json");
                Manager.LoadCommandsFromJson(cmdJson);
            }

            // Existing Layout Load
            if (File.Exists("user_layout.json"))
            {
                string json = File.ReadAllText("user_layout.json");
                Manager.LoadLayoutFromJson(json);
            }
            else if (File.Exists("DefaultLayout.json"))
            {
                string json = File.ReadAllText("DefaultLayout.json");
                Manager.LoadLayoutFromJson(json);
            }
        }
    }
}