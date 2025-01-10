using System;
using System.Windows;
using RB4InstrumentMapper.Properties;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            autoStartCheckBox.IsChecked = Settings.Default.autoStart;
            verboseLogCheckBox.IsChecked = Settings.Default.verboseLogging;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void saveButton_Click(object sender, RoutedEventArgs args)
        {
            Settings.Default.autoStart = autoStartCheckBox.IsChecked.GetValueOrDefault();

            Settings.Default.verboseLogging = verboseLogCheckBox.IsChecked.GetValueOrDefault();
            Logging.PrintVerbose = Settings.Default.verboseLogging;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
