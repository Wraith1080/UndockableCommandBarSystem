using System.IO;
using System.Windows;
using CommandBar.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace CommandBar.DemoApp
{
    public partial class MainViewModel : ObservableObject
    {
        public CommandBarManager Manager { get; } = new();

        // Instead of a single Toolbar, the UI will bind to the Manager's list of active toolbars
        public IReadOnlyList<ToolbarModel> Toolbars => Manager.ActiveToolbars;

        public MainViewModel()
        {
            // 1. POPULATE THE MASTER REGISTRY (One time only)
            RegisterAllAppCommands();

            // 2. LOAD THE JSON LAYOUT FILE
            if (File.Exists("DefaultLayout.json"))
            {
                string json = File.ReadAllText("DefaultLayout.json");
                Manager.LoadLayoutFromJson(json);
            }
        }

        private void RegisterAllAppCommands()
        {
            string saveIconPath = "M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3M19,19H5V5H16.17L19,7.83V19M12,12A3,3 0 0,0 9,15A3,3 0 0,0 12,18A3,3 0 0,0 15,15A3,3 0 0,0 12,12M12,10.5C10.89,10.5 10,11.39 10,12.5C10,13.6 10.89,14.5 12,14.5C13.11,14.5 14,13.6 14,12.5C14,11.39 13.11,10.5 12,10.5M15,9H5V5H15V9Z";
            string boldIconPath = "M13.5,15.5H10V12.5H13.5A1.5,1.5 0 0,1 15,14A1.5,1.5 0 0,1 13.5,15.5M10,6.5H13A1.5,1.5 0 0,1 14.5,8A1.5,1.5 0 0,1 13,9.5H10M15.6,10.79C16.57,10.11 17.25,9 17.25,8C17.25,5.74 15.5,4 13.25,4H7V18H14.04C16.14,18 17.75,16.3 17.75,14.21C17.75,12.69 16.89,11.39 15.6,10.79Z";

            // 1. Register base commands (Notice Save is now ImageOnly!)
            Manager.RegisterCommand("App.Save", new CommandItem { Text = "Save", DisplayMode = CommandDisplayMode.ImageOnly, Tooltip = "Save Document", IconGeometry = saveIconPath, ActionCallback = () => MessageBox.Show("Saved!") });
            Manager.RegisterCommand("App.Bold", new CommandToggleItem { Text = "Bold", Tooltip = "Bold Text", IconGeometry = boldIconPath });
            Manager.RegisterCommand("App.Separator", new CommandSeparator());

            // 2. Build a Dropdown Menu!
            var fileMenu = new CommandDropdownItem { Text = "File", DisplayMode = CommandDisplayMode.TextOnly };

            // 3. Clone the Save button from the registry and inject it INSIDE the File Menu!
            var saveCloneForMenu = Manager.GetCommand("App.Save");
            if (saveCloneForMenu != null)
            {
                // We override its display mode inside the menu so it shows text alongside the icon
                saveCloneForMenu.DisplayMode = CommandDisplayMode.ImageAndText;
                fileMenu.ChildItems.Add(saveCloneForMenu);
            }

            Manager.RegisterCommand("App.FileMenu", fileMenu);
        }
    }
}