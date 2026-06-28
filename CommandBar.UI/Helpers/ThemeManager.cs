using System;
using System.Windows;

namespace CommandBar.UI
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            // 🟢 Explicitly tell C# to use the WPF Application, not the network Application!
            var appResources = System.Windows.Application.Current.Resources;

            // 1. Find and remove any existing CommandBar themes
            ResourceDictionary? existingTheme = null;
            foreach (var dict in appResources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.ToString().Contains("Theme"))
                {
                    existingTheme = dict;
                    break;
                }
            }
            if (existingTheme != null)
            {
                appResources.MergedDictionaries.Remove(existingTheme);
            }

            // 2. Load the new requested theme!
            try
            {
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/CommandBar.UI;component/Themes/{themeName}.xaml", UriKind.Absolute)
                };
                appResources.MergedDictionaries.Add(newTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme {themeName}: {ex.Message}");
            }
        }

        public static void ChangeTheme(string themeName)
        {
            var app = Application.Current;
            if (app == null) return;

            // 1. Find the existing theme dictionary (if any) and remove it
            var existingTheme = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme"));

            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }

            // 2. Load the new theme dictionary
            try
            {
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/CommandBar.UI;component/Themes/Theme{themeName}.xaml")
                };

                // 3. Inject it into the application
                app.Resources.MergedDictionaries.Add(newTheme);
            }
            catch (Exception ex)
            {
                // Fallback or log if the theme name doesn't exist
                System.Diagnostics.Debug.WriteLine($"Failed to load theme: {themeName}. Error: {ex.Message}");
            }
        }
    }
}