using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandBar.Core.Models
{
    public partial class CommandToggleItem : CommandItem
    {
        [ObservableProperty]
        private bool _isChecked;

        // NEW: Override the base clone method
        public override CommandItem Clone()
        {
            return new CommandToggleItem
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback,

                // Copy the specific Toggle property
                IsChecked = this.IsChecked
            };
        }
    }
}