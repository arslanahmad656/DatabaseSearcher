using DatabaseSearcher.App.Dto;
using DatabaseSearcher.App.Helpers;
using SqlSearcher = SQLServerSearcher.SQLServerSearcher;
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
using DatabaseSearcher.Dto.Status;

namespace DatabaseSearcher.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? previousConnectionString;
        private CancellationTokenSource? cancellationToken;

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
            Btn_Search.IsEnabled = !string.IsNullOrWhiteSpace(Txt_SearchText.Text);
        }

        private async void Txt_ConnectionString_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.Equals(Txt_ConnectionString.Text, previousConnectionString))
            {
                previousConnectionString = Txt_ConnectionString.Text;
                await SaveConfigurations();
            }
        }

        private void Txt_SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            Btn_Search.IsEnabled = !string.IsNullOrWhiteSpace(Txt_SearchText.Text);
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationToken is not null)
            {
                Btn_Stop.IsEnabled = false;
                cancellationToken.Cancel();
            }
        }

        private async void Btn_Search_Click(object sender, RoutedEventArgs e)
        {
            SqlSearcher searcher = new (Txt_ConnectionString.Text);
            try
            {
                Btn_Search.IsEnabled = false;
                Btn_Stop.IsEnabled = true;
                Txt_Logs.Text = "";
                Txt_Result.Text = "";

                string activeTableName = string.Empty;
                bool isFirstTimeReporting = true;

                var progressReporter = new Progress<Status>(st =>
                {
                    Pb_Status.Value = Math.Ceiling(st.PercentageProcessed);

                    if (isFirstTimeReporting)
                    {
                        Txt_Logs.Text += $"Total {st.TotalTablesStatus.Total} tables to search.";
                        isFirstTimeReporting = false;
                    }

                    if (!string.Equals(activeTableName, st.ActiveTableStatus.TableName))
                    {
                        activeTableName = st.ActiveTableStatus.TableName;
                        Tb_Status.Text = $"Searching in {st.ActiveTableStatus.TableName}";
                        Txt_Logs.Text += $"Searching in {st.ActiveTableStatus.TableName}. (Total {st.ActiveTableStatus.TotalRows} rows.){Environment.NewLine}{st.TotalTablesStatus.Total - st.TotalTablesStatus.Processed} tables reamining.{Environment.NewLine}";
                    }
                    else
                    {
                        if (st.ActiveTableStatus.RowsProcessed % 5000 == 0)
                        {
                            Txt_Logs.Text += $"{st.ActiveTableStatus.TotalRows - st.ActiveTableStatus.RowsProcessed} rows left.{Environment.NewLine}";
                        }
                    }
                });

                cancellationToken = new();
                await foreach (var (table, column, rowNumber) in searcher.Search(Txt_SearchText.Text, progressReporter, cancellationToken.Token))
                {
                    Txt_Result.Text += $"Found a match in {table} table inside column {column} at row number {rowNumber}.{Environment.NewLine}";
                }
            }
            catch(OperationCanceledException)
            {
                MessageBox.Show("Search has been cancelled.", "Search Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                await searcher.DisposeAsync();

                Btn_Search.IsEnabled = true;
                Btn_Stop.IsEnabled = false;

                cancellationToken = null;
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