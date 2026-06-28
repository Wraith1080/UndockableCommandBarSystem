using System;
using System.Windows;

namespace CommandBar.DemoApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 🟢 NEW: Wait until the window is fully built before loading the layout!
            this.Loaded += (s, e) =>
            {
                if (this.DataContext is MainViewModel vm)
                {
                    vm.InitializeLayout();
                }
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (this.DataContext is MainViewModel vm)
            {
                vm.SaveCurrentLayout();
            }
        }
    }
}