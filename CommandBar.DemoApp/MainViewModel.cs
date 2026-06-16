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
            MainToolbar = Manager.CreateToolbar("Standard Toolbar");

            // A standard SVG path for a "Save" Floppy Disk icon
            string saveIconPath = "M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3M19,19H5V5H16.17L19,7.83V19M12,12A3,3 0 0,0 9,15A3,3 0 0,0 12,18A3,3 0 0,0 15,15A3,3 0 0,0 12,12M12,10.5C10.89,10.5 10,11.39 10,12.5C10,13.6 10.89,14.5 12,14.5C13.11,14.5 14,13.6 14,12.5C14,11.39 13.11,10.5 12,10.5M15,9H5V5H15V9Z";

            var btnSave = new CommandItem
            {
                Text = "Save",
                Tooltip = "Save Document",
                IconGeometry = saveIconPath
            };
            btnSave.ActionCallback = () => MessageBox.Show("Save Command Executed!");

            // A standard SVG path for a "Bold" Text icon
            string boldIconPath = "M13.5,15.5H10V12.5H13.5A1.5,1.5 0 0,1 15,14A1.5,1.5 0 0,1 13.5,15.5M10,6.5H13A1.5,1.5 0 0,1 14.5,8A1.5,1.5 0 0,1 13,9.5H10M15.6,10.79C16.57,10.11 17.25,9 17.25,8C17.25,5.74 15.5,4 13.25,4H7V18H14.04C16.14,18 17.75,16.3 17.75,14.21C17.75,12.69 16.89,11.39 15.6,10.79Z";

            var btnBold = new CommandToggleItem
            {
                Text = "Bold",
                Tooltip = "Toggle Bold Text",
                IconGeometry = boldIconPath
            };

            var separator = new CommandSeparator();

            Manager.RegisterCommand(btnSave);
            Manager.RegisterCommand(btnBold);
            Manager.RegisterCommand(separator);

            MainToolbar.DockedItems.Add(btnSave);
            MainToolbar.DockedItems.Add(separator);
            MainToolbar.DockedItems.Add(btnBold);

            // NEW: Spam the toolbar with extra buttons to test the overflow limit!
            for (int i = 1; i <= 15; i++)
            {
                var extraBtn = new CommandItem
                {
                    Text = $"Tool {i}",
                    IconGeometry = saveIconPath // Just reuse the save icon for testing
                };
                Manager.RegisterCommand(extraBtn);
                MainToolbar.DockedItems.Add(extraBtn);
            }
        }
    }
}