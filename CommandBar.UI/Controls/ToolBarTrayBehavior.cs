using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommandBar.Core.Models;

namespace CommandBar.UI.Controls
{
    public static class ToolBarTrayBehavior
    {
        // Invent a brand new "ItemsSource" property for the ToolBarTray
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(ToolBarTrayBehavior), new PropertyMetadata(null, OnItemsSourceChanged));

        public static void SetItemsSource(DependencyObject element, IEnumerable value) => element.SetValue(ItemsSourceProperty, value);
        public static IEnumerable GetItemsSource(DependencyObject element) => (IEnumerable)element.GetValue(ItemsSourceProperty);

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToolBarTray tray)
            {
                tray.ToolBars.Clear();

                // 1. Initial Load: Loop through the ViewModel's Toolbars and create UI elements
                if (e.NewValue is IEnumerable collection)
                {
                    foreach (ToolbarModel model in collection)
                    {
                        tray.ToolBars.Add(CreateToolBar(model));
                    }
                }

                // 2. Dynamic Updates: If the JSON loads later, rebuild the tray!
                if (e.NewValue is INotifyCollectionChanged newObservable)
                {
                    newObservable.CollectionChanged += (sender, args) =>
                    {
                        tray.ToolBars.Clear();
                        foreach (ToolbarModel model in (IEnumerable)sender)
                        {
                            tray.ToolBars.Add(CreateToolBar(model));
                        }
                    };
                }
            }
        }

        // Helper method to automatically bind the layout properties
        private static UndockableToolBar CreateToolBar(ToolbarModel model)
        {
            var toolBar = new UndockableToolBar();

            // NEW FIX: Stop the C#-generated toolbar from stretching across the screen!
            toolBar.HorizontalAlignment = HorizontalAlignment.Left;

            // Bind the Row (Band) and Position (BandIndex) so dragging saves the data
            toolBar.SetBinding(ToolBar.BandProperty, new Binding("Band") { Source = model, Mode = BindingMode.TwoWay });
            toolBar.SetBinding(ToolBar.BandIndexProperty, new Binding("BandIndex") { Source = model, Mode = BindingMode.TwoWay });

            // Bind the Buttons
            toolBar.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DockedItems") { Source = model });

            return toolBar;
        }
    }
}