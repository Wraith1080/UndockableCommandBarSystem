using CommandBar.Core.Models;
using System.Windows;

namespace CommandBar.DemoApp
{
    public class MainViewModel
    {
        // The core manager we built earlier
        public CommandBarManager Manager { get; } = new CommandBarManager();

        // The specific toolbar we want to show on the screen
        public ToolbarModel MainToolbar { get; }

        public MainViewModel()
        {
            // 1. Create a new toolbar
            MainToolbar = Manager.CreateToolbar("Standard Toolbar");

            // 2. Create some test items
            var btnSave = new CommandItem { Text = "Save", Tooltip = "Save Document" };
            btnSave.ActionCallback = () => MessageBox.Show("Save Command Executed!");

            var btnBold = new CommandToggleItem { Text = "Bold", Tooltip = "Toggle Bold Text" };

            var separator = new CommandSeparator();

            // 3. Register them to the Master list (for the future Customize Dialog)
            Manager.RegisterCommand(btnSave);
            Manager.RegisterCommand(btnBold);
            Manager.RegisterCommand(separator);

            // 4. Dock them into our active toolbar
            MainToolbar.DockedItems.Add(btnSave);
            MainToolbar.DockedItems.Add(separator);
            MainToolbar.DockedItems.Add(btnBold);
        }
    }
}