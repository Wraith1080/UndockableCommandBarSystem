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

        // NEW: Tells the UI if this specific toolbar should act as a Main Menu Bar
        [ObservableProperty]
        private bool _isMenuBar;

        // NEW: Tracks which side of the screen this toolbar lives on
        [ObservableProperty]
        private DockLocation _dockLocation = DockLocation.Top;

        private double _floatingLeft;
        public double FloatingLeft
        {
            get => _floatingLeft;
            set => SetProperty(ref _floatingLeft, value);
        }

        private double _floatingTop;
        public double FloatingTop
        {
            get => _floatingTop;
            set => SetProperty(ref _floatingTop, value);
        }

        // The actual buttons currently sitting inside this specific toolbar
        public ObservableCollection<CommandItem> DockedItems { get; } = new();

        // NEW: An event to tell the Manager that this toolbar wants to move!
        public event Action<ToolbarModel, DockLocation>? DockChangeRequested;

        public void RequestDockChange(DockLocation newDock)
        {
            DockChangeRequested?.Invoke(this, newDock);
        }
    }
}