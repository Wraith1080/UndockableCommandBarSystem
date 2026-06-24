using CommandBar.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
namespace CommandBar.Core.Models
{
    public partial class CommandItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private string _tooltip = string.Empty;

        [ObservableProperty]
        private string _iconGeometry = string.Empty;

        [ObservableProperty]
        private string _shortcutText = string.Empty; // e.g., "Ctrl+S"

        // NEW: Controls how the UI renders this specific button
        [ObservableProperty]
        private CommandDisplayMode _displayMode = CommandDisplayMode.ImageAndText;

        public Action? ActionCallback { get; set; }
        public ICommand ExecuteItemCommand => new RelayCommand(() => ActionCallback?.Invoke());

        // NEW: Allows native menus to know if this is a separator without using a DataTemplate
        public virtual bool IsSeparator => false;

        // Add this to your existing CommandItem.cs
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public virtual CommandItem Clone()
        {
            return new CommandItem
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback,

                // NEW: Preserve the display mode during cloning!
                DisplayMode = this.DisplayMode
            };
        }
    }
}