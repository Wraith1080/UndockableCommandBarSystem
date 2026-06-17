using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json; // REQUIRED FOR JSON

namespace CommandBar.Core.Models
{
    public class CommandBarManager
    {
        // THE MASTER REGISTRY: Holds the pure definition of every button/action in the app
        private readonly Dictionary<string, CommandItem> _masterCommandRegistry = new();

        // REPLACE _activeToolbars WITH THESE 4 LISTS
        public ObservableCollection<ToolbarModel> TopToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> BottomToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> LeftToolbars { get; } = new();
        public ObservableCollection<ToolbarModel> RightToolbars { get; } = new();

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

                // 3. Loop through the string IDs in the JSON
                foreach (var commandId in tbConfig.Items)
                {
                    // 4. Look up the ID in the Master Registry, clone it, and inject it!
                    var commandToInject = GetCommand(commandId);
                    if (commandToInject != null)
                    {
                        toolbar.DockedItems.Add(commandToInject);
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

            toolbar.DockLocation = newDock;

            switch (newDock)
            {
                case DockLocation.Top: TopToolbars.Add(toolbar); break;
                case DockLocation.Bottom: BottomToolbars.Add(toolbar); break;
                case DockLocation.Left: LeftToolbars.Add(toolbar); break;
                case DockLocation.Right: RightToolbars.Add(toolbar); break;
            }
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
        public string Dock { get; set; } = "Top"; // NEW: JSON will pass "Top", "Left", etc.
        public List<string> Items { get; set; } = new();
    }
}