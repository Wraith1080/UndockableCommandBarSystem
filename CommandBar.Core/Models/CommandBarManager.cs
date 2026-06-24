using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json; // REQUIRED FOR JSON
using System.Linq;

namespace CommandBar.Core.Models
{
    public partial class CommandBarManager : ObservableObject
    {
        // THE MASTER REGISTRY: Holds the pure definition of every button/action in the app
        private readonly Dictionary<string, CommandItem> _masterCommandRegistry = new();

        // REPLACE _activeToolbars WITH THESE 4 LISTS
        public ObservableCollection<ToolbarModel> TopToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> BottomToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> LeftToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> RightToolbars { get; } = new();

        // 🟢 NEW: The Missing Floating List & UI Trigger
        public ObservableCollection<ToolbarModel> FloatingToolbars { get; } = new();
        public Action<ToolbarModel>? RestoreFloatingWindowAction { get; set; }

        // The Master Switch that unlocks toolbars for dragging
        [ObservableProperty]
        private bool _isCustomizeMode = false;

        // NEW: A delegate that the UI layer will hook into
        public Action? OpenCustomizeDialogAction { get; set; }

        public System.Collections.Generic.IEnumerable<CommandItem> AvailableCommands => _masterCommandRegistry.Values;

        /// <summary>
        /// Registers a command into the master dictionary. 
        /// This is called ONCE at application startup.
        /// </summary>
        public void RegisterCommand(string commandId, CommandItem item)
        {
            if (string.IsNullOrWhiteSpace(commandId)) throw new ArgumentException("Command ID cannot be empty.");

            // Store the ID inside the item so it knows its own identity
            item.Id = commandId;

            _masterCommandRegistry[commandId] = item;
        }

        /// <summary>
        /// Fetches a CLONE of a command from the registry so it can be safely placed in a toolbar.
        /// </summary>
        public CommandItem? GetCommand(string commandId)
        {
            if (_masterCommandRegistry.TryGetValue(commandId, out var templateItem))
            {
                // We return a CLONE so that the same button can exist in multiple toolbars safely!
                return templateItem.Clone();
            }
            return null;
        }

        /// <summary>
        /// Creates a new empty toolbar and adds it to the active layout.
        /// </summary>
        public ToolbarModel CreateToolbar(string name, DockLocation dock = DockLocation.Top, int row = 0, int index = 0, bool isMenuBar = false)
        {
            var toolbar = new ToolbarModel
            {
                Name = name,
                DockLocation = dock,
                Band = row,
                BandIndex = index,
                IsMenuBar = isMenuBar
            };

            toolbar.DockChangeRequested += MoveToolbar;

            // Route the toolbar to the correct UI list!
            switch (dock)
            {
                case DockLocation.Top: TopToolbars.Add(toolbar); break;
                case DockLocation.Bottom: BottomToolbars.Add(toolbar); break;
                case DockLocation.Left: LeftToolbars.Add(toolbar); break;
                case DockLocation.Right: RightToolbars.Add(toolbar); break;
                case DockLocation.Floating: FloatingToolbars.Add(toolbar); break; // NEW
            }

            return toolbar;
        }

        // --- NEW: THE JSON DESERIALIZATION ENGINE ---
        public void LoadLayoutFromJson(string jsonString)
        {
            TopToolbars.Clear();
            BottomToolbars.Clear();
            LeftToolbars.Clear();
            RightToolbars.Clear();

            // 1. Read the JSON text into our temporary C# objects
            var layoutOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var layout = JsonSerializer.Deserialize<LayoutFileDto>(jsonString, layoutOptions);

            if (layout?.Toolbars == null) return;

            // 2. Loop through the JSON toolbars
            foreach (var tbConfig in layout.Toolbars)
            {
                Enum.TryParse(tbConfig.Dock, true, out DockLocation parsedDock);
                var toolbar = CreateToolbar(tbConfig.Name, parsedDock, tbConfig.Band, tbConfig.BandIndex, tbConfig.IsMenuBar);

                // 🟢 NEW: Restore coordinates and trigger the window spawn!
                toolbar.FloatingLeft = tbConfig.FloatingLeft;
                toolbar.FloatingTop = tbConfig.FloatingTop;

                if (parsedDock == DockLocation.Floating)
                {
                    RestoreFloatingWindowAction?.Invoke(toolbar);
                }

                // Inside LoadLayoutFromJson(), replacing step 3 & 4:
                foreach (var commandId in tbConfig.Items)
                {
                    var commandToInject = GetCommand(commandId);
                    if (commandToInject != null)
                    {
                        // NEW: Did the user hide this button during their last session?
                        if (tbConfig.HiddenItems != null && tbConfig.HiddenItems.Contains(commandId))
                        {
                            commandToInject.IsVisible = false;
                        }

                        toolbar.DockedItems.Add(commandToInject);
                    }
                }
            }
        }

        // --- NEW: THE JSON SERIALIZATION ENGINE ---
        public string SaveLayoutToJson()
        {
            var layout = new LayoutFileDto();

            // Local helper function to process each dock collection
            void ProcessToolbars(IEnumerable<ToolbarModel> toolbars)
            {
                foreach (var tb in toolbars)
                {
                    var config = new ToolbarConfigDto
                    {
                        Name = tb.Name,
                        Band = tb.Band,
                        BandIndex = tb.BandIndex,
                        IsMenuBar = tb.IsMenuBar,
                        Dock = tb.DockLocation.ToString(),
                        FloatingLeft = tb.FloatingLeft, // NEW
                        FloatingTop = tb.FloatingTop,   // NEW
                        Items = new List<string>(),
                        HiddenItems = new List<string>() // Initialize it
                    };

                    foreach (var item in tb.DockedItems)
                    {
                        if (!string.IsNullOrEmpty(item.Id))
                        {
                            config.Items.Add(item.Id);

                            // NEW: If they unchecked it, log it!
                            if (!item.IsVisible)
                            {
                                config.HiddenItems.Add(item.Id);
                            }
                        }
                    }

                    layout.Toolbars.Add(config);
                }
            }

            // Process all 4 zones
            ProcessToolbars(TopToolbars);
            ProcessToolbars(BottomToolbars);
            ProcessToolbars(LeftToolbars);
            ProcessToolbars(RightToolbars);
            ProcessToolbars(FloatingToolbars);

            // Serialize with nice indentation so users can read/edit it manually if they want to
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(layout, options);
        }

        // --- NEW: THE RESET ENGINE ---
        public void ResetToolbar(ToolbarModel toolbar, string defaultJsonString)
        {
            if (toolbar == null || string.IsNullOrWhiteSpace(defaultJsonString)) return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var layout = JsonSerializer.Deserialize<LayoutFileDto>(defaultJsonString, options);

            if (layout?.Toolbars == null) return;

            // Find the "Factory Settings" for this specific toolbar
            var defaultConfig = layout.Toolbars.FirstOrDefault(t => t.Name == toolbar.Name);
            if (defaultConfig != null)
            {
                // 1. Reset Position
                toolbar.Band = defaultConfig.Band;
                toolbar.BandIndex = defaultConfig.BandIndex;
                Enum.TryParse(defaultConfig.Dock, true, out DockLocation parsedDock);

                // If the dock changed, request a move so the UI updates!
                if (toolbar.DockLocation != parsedDock)
                {
                    toolbar.RequestDockChange(parsedDock);
                }

                // 2. Reset Buttons
                toolbar.DockedItems.Clear();
                foreach (var commandId in defaultConfig.Items)
                {
                    var cmd = GetCommand(commandId);
                    if (cmd != null)
                    {
                        cmd.IsVisible = true; // Ensure it is un-hidden
                        toolbar.DockedItems.Add(cmd);
                    }
                }
            }
        }

        // NEW: Plucks the toolbar out of its old list and puts it in the new one!
        private void MoveToolbar(ToolbarModel toolbar, DockLocation newDock)
        {
            TopToolbars.Remove(toolbar);
            BottomToolbars.Remove(toolbar);
            LeftToolbars.Remove(toolbar);
            RightToolbars.Remove(toolbar);
            FloatingToolbars.Remove(toolbar); // NEW

            toolbar.DockLocation = newDock;

            switch (newDock)
            {
                case DockLocation.Top: TopToolbars.Add(toolbar); break;
                case DockLocation.Bottom: BottomToolbars.Add(toolbar); break;
                case DockLocation.Left: LeftToolbars.Add(toolbar); break;
                case DockLocation.Right: RightToolbars.Add(toolbar); break;
                case DockLocation.Floating: FloatingToolbars.Add(toolbar); break; // NEW
            }
        }

        public void ShowCustomizeDialog()
        {
            // 1. Unlock the toolbars
            this.IsCustomizeMode = true;

            // 2. Tell the UI layer to physically open the window (if it is hooked up)
            OpenCustomizeDialogAction?.Invoke();
        }
    }

    // --- NEW: LIGHTWEIGHT DTOs FOR JSON PARSING ---
    public class LayoutFileDto
    {
        public List<ToolbarConfigDto> Toolbars { get; set; } = new();
    }

    public class ToolbarConfigDto
    {
        public string Name { get; set; } = string.Empty;
        public int Band { get; set; }
        public int BandIndex { get; set; }
        public bool IsMenuBar { get; set; }
        public string Dock { get; set; } = "Top";
        public List<string> Items { get; set; } = new();

        // NEW: Keep track of buttons the user unchecked!
        public List<string> HiddenItems { get; set; } = new();

        public double FloatingLeft { get; set; } // NEW
        public double FloatingTop { get; set; }  // NEW
    }
}