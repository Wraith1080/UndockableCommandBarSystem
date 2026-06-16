using System.Collections.ObjectModel;

namespace CommandBar.Core.Models
{
    public partial class CommandDropdownItem : CommandItem
    {
        // Holds the child items (which can be standard buttons, toggles, or even more dropdowns)
        public ObservableCollection<CommandItem> ChildItems { get; } = new ObservableCollection<CommandItem>();

        public CommandDropdownItem()
        {
            // Dropdowns usually don't have a direct click action that executes a command, 
            // they just open the flyout menu containing the ChildItems.
        }
    }
}