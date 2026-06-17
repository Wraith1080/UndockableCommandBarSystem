using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CommandBar.Core.Models
{
    // Inheriting from ObservableObject gives us INotifyPropertyChanged automatically
    public partial class CommandItem : ObservableObject
    {
        // NEW: The unique string ID (e.g., "App.Save")
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private string _tooltip = string.Empty;

        [ObservableProperty]
        private string _iconGeometry = string.Empty; // Holds Vector Path Data for crisp scaling

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExecuteItemCommand))]
        private bool _isEnabled = true;

        [ObservableProperty]
        private bool _isVisible = true;

        // The actual action to run when clicked
        public Action? ActionCallback { get; set; }

        // The RelayCommand generates our ICommand binding for the WPF UI
        [RelayCommand(CanExecute = nameof(CanExecuteItem))]
        private void ExecuteItem()
        {
            ActionCallback?.Invoke();
        }

        private bool CanExecuteItem()
        {
            return IsEnabled;
        }
        // NEW: A method to safely clone the button for the UI
        public virtual CommandItem Clone()
        {
            return new CommandItem
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback
            };
        }
    }
}