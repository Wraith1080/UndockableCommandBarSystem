using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommandBar.Core.Models
{
    public partial class ToolbarModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        // The Row the toolbar sits on
        [ObservableProperty]
        private int _band;

        // The horizontal position of the toolbar in that row
        [ObservableProperty]
        private int _bandIndex;

        // The actual buttons currently sitting inside this specific toolbar
        public ObservableCollection<CommandItem> DockedItems { get; } = new();
    }
}