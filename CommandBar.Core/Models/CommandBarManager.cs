using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace CommandBar.Core.Models
{
    public partial class CommandBarManager : ObservableObject
    {
        // The master dictionary of ALL available commands in the application.
        // The Customize Dialog will read from this to show users what they can drag into their toolbars.
        public ObservableCollection<CommandItem> MasterItems { get; } = new ObservableCollection<CommandItem>();

        // The collection of active toolbars currently deployed in the UI.
        public ObservableCollection<ToolbarModel> ActiveToolbars { get; } = new ObservableCollection<ToolbarModel>();

        // A helper method to register a new command into the master list
        public void RegisterCommand(CommandItem item)
        {
            if (!MasterItems.Any(x => x.Id == item.Id))
            {
                MasterItems.Add(item);
            }
        }

        // A helper method to create a new, empty toolbar
        public ToolbarModel CreateToolbar(string toolbarName)
        {
            var newToolbar = new ToolbarModel { Name = toolbarName };
            ActiveToolbars.Add(newToolbar);
            return newToolbar;
        }
    }

    // A simple model to represent an individual ToolBar (which contains its own collection of items)
    public partial class ToolbarModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = "New Toolbar";

        [ObservableProperty]
        private bool _isFloating = false;

        // The items actively placed inside this specific toolbar
        public ObservableCollection<CommandItem> DockedItems { get; } = new ObservableCollection<CommandItem>();
    }
}