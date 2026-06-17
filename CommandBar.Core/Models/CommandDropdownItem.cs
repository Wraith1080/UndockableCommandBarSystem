using System.Collections.ObjectModel;

namespace CommandBar.Core.Models
{
    public class CommandDropdownItem : CommandItem
    {
        public ObservableCollection<CommandItem> ChildItems { get; } = new();

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