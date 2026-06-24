using System.Collections.ObjectModel;

namespace CommandBar.Core.Models
{
    public class CommandDropdownItem : CommandItem
    {
        // We keep it read-only (no set;), but we initialize it immediately!
        public ObservableCollection<CommandItem> ChildItems { get; } = new ObservableCollection<CommandItem>();

        public override CommandItem Clone()
        {
            var clone = new CommandDropdownItem
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback,
                DisplayMode = this.DisplayMode
            };

            // CRITICAL: Recursively clone every child item inside the menu!
            foreach (var child in this.ChildItems)
            {
                clone.ChildItems.Add(child.Clone());
            }

            return clone;
        }
    }
}