using System;
using System.Collections.Generic;
using System.Text.Json; // REQUIRED FOR JSON

namespace CommandBar.Core.Models
{
    public class CommandBarManager
    {
        // THE MASTER REGISTRY: Holds the pure definition of every button/action in the app
        private readonly Dictionary<string, CommandItem> _masterCommandRegistry = new();

        // THE ACTIVE LAYOUT: Holds the toolbars currently visible on the screen
        private readonly List<ToolbarModel> _activeToolbars = new();
        public IReadOnlyList<ToolbarModel> ActiveToolbars => _activeToolbars;

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
        public ToolbarModel CreateToolbar(string name, int row = 0, int index = 0, bool isMenuBar = false)
        {
            var toolbar = new ToolbarModel
            {
                Name = name,
                Band = row,
                BandIndex = index,
                IsMenuBar = isMenuBar // NEW
            };
            _activeToolbars.Add(toolbar);
            return toolbar;
        }

        // --- NEW: THE JSON DESERIALIZATION ENGINE ---
        public void LoadLayoutFromJson(string jsonString)
        {
            _activeToolbars.Clear();

            // 1. Read the JSON text into our temporary C# objects
            var layoutOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var layout = JsonSerializer.Deserialize<LayoutFileDto>(jsonString, layoutOptions);

            if (layout?.Toolbars == null) return;

            // 2. Loop through the JSON toolbars
            foreach (var tbConfig in layout.Toolbars)
            {
                var toolbar = CreateToolbar(tbConfig.Name, tbConfig.Band, tbConfig.BandIndex);

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
        public List<string> Items { get; set; } = new();

        public bool IsMenuBar { get; set; } // NEW
    }
}