using System.Collections.ObjectModel;

namespace CommandBar.Core.Models
{
    public class CommandDropdownItem : CommandItem
    {
        // We keep it read-only (no set;), but we initialize it immediately!
        public ObservableCollection<CommandItem> ChildItems { get; } = new ObservableCollection<CommandItem>();

        // 🟢 NEW: Deep copy the entire menu tree during a clone!
        public override CommandItem Clone()
        {
            var clone = new CommandDropdownItem
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback,
                DisplayMode = this.DisplayMode,
                KeepOriginalColors = this.KeepOriginalColors,
                RawSvgContent = this.RawSvgContent,
                IsVisible = this.IsVisible
            };

            foreach (var child in this.ChildItems)
            {
                clone.ChildItems.Add(child.Clone());
            }

            return clone;
        }
    }
}