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
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void saveButton_Click(object sender, RoutedEventArgs args)
        {
            Settings.Default.autoStart = autoStartCheckBox.IsChecked.GetValueOrDefault();

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
