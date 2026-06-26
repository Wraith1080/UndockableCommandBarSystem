using System.Windows;
using CommandBar.UI; // 🟢 Ensures we can see the ThemeManager!

namespace CommandBar.DemoApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 🟢 APPLY THE THEME: Load the dictionary before the MainWindow opens!
            ThemeManager.ApplyTheme("ThemeModern");
        }
    }
}