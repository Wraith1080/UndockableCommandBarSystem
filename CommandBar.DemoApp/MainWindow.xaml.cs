using System;
using System.Windows;

namespace CommandBar.DemoApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Grab the ViewModel and tell it to save the layout before shutting down!
            if (this.DataContext is MainViewModel vm)
            {
                vm.SaveCurrentLayout();
            }
        }
    }
}