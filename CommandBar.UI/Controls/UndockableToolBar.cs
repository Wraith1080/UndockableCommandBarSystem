using System.Windows;
using System.Windows.Controls;

namespace CommandBar.UI.Controls
{
    public class UndockableToolBar : ItemsControl
    {
        static UndockableToolBar()
        {
            // This tells WPF to look for our default style in Themes/Generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(UndockableToolBar),
                new FrameworkPropertyMetadata(typeof(UndockableToolBar)));
        }

        // Later, we will add Dependency Properties here (like IsFloating or ToolbarName)
        // For now, inheriting from ItemsControl is all we need to bind our collections.
    }
}