using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PingPlotter
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            textBoxHost.Focus();
        }

        private void startPing(object sender, RoutedEventArgs e)
        {
            string csvFile;
            if ((bool)CheckCSVSave.IsChecked) {
                csvFile = textBoxCSVPath.Text;
            }
            else
            {
                csvFile = null;
            }

            GraphWindow graphWindow = new GraphWindow(textBoxHost.Text, csvFile);
            graphWindow.Show();
            Close();
        }

        private void CSVCheckChanged(object sender, RoutedEventArgs e)
        {
            lblCSVPath.IsEnabled = (bool)CheckCSVSave.IsChecked;
            textBoxCSVPath.IsEnabled = (bool)CheckCSVSave.IsChecked;
        }

        private void textBoxHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCSVPath.Text = string.Concat("Pinglog_", textBoxHost.Text, ".csv");
        }

        private void textBoxHost_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                startPing(null, null);
            }
        }
    }
}
