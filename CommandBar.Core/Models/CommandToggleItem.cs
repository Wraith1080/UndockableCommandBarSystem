using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandBar.Core.Models
{
    public partial class CommandToggleItem : CommandItem
    {
        [ObservableProperty]
        private bool _isChecked;

        // You can optionally intercept the state change if the manager needs to know immediately
        partial void OnIsCheckedChanged(bool value)
        {
            // Logic to handle when the toggle state changes, 
            // separate from the actual ExecuteItem command if needed.
        }
    }
}