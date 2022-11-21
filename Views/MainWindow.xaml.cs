using System;
using System.Windows;
using System.Windows.Input;

namespace TroveSkip.Views
{
    public partial class MainWindow
    {
        private ViewModels.MainWindowViewModel _viewModel;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _viewModel = DataContext as ViewModels.MainWindowViewModel;
                // Closing += (_, _) =>
                // {
                //     _viewModel.SaveCurrent();
                //     _viewModel.Settings.Save();
                // };
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        private void WindowMouseDown(object sender, MouseButtonEventArgs args) => WindowDeactivated(sender, args);

        private void WindowDeactivated(object sender, EventArgs args) => _viewModel.WindowDeactivated(sender, args);

        private void DragWindow(object s, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        // private void ClickComboBox(object sender, MouseButtonEventArgs e) => _viewModel.RefreshHooks(true);
    }
}