using DatabaseSearcher.App.Dto;
using DatabaseSearcher.App.Helpers;
using System.DirectoryServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DatabaseSearcher.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? previousConnectionString;

        public MainWindow()
        {
            InitializeComponent();
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleError(e.Exception);
            e.Handled = true;
        }

        private async void Window_Main_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadConfigurations();
        }

        private async void Txt_ConnectionString_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.Equals(Txt_ConnectionString.Text, previousConnectionString))
            {
                previousConnectionString = Txt_ConnectionString.Text;
                await SaveConfigurations();
            }
        }

        #region Helpers

        private async Task LoadConfigurations()
        {
            var configs = await ConfigHelper.LoadConfigurations();
            if (configs is not null)
            {
                Txt_ConnectionString.Text = configs.ConnectionString;
                previousConnectionString = configs.ConnectionString;
            }
        }

        private async Task SaveConfigurations()
        {
            var configs = new SavedConfiguration(Txt_ConnectionString.Text);
            await ConfigHelper.SaveConfigurations(configs);
        }

        private void HandleError(Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}