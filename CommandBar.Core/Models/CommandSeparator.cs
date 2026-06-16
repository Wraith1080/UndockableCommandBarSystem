using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandBar.Core.Models
{
    public partial class CommandSeparator : CommandItem
    {
        // We override or ignore the ActionCallback and Text since it's just a visual divider.
        public CommandSeparator()
        {
            Text = "Separator";
            IsEnabled = false; // Prevents hover/click states in the UI
        }
    }
}