using System;
using System.Windows;
using RB4InstrumentMapper.GUI.Properties;

namespace RB4InstrumentMapper.GUI
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
            accurateDrumMapsCheckBox.IsChecked = Settings.Default.accurateDrumMaps;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void saveButton_Click(object sender, RoutedEventArgs args)
        {
            Settings.Default.autoStart = autoStartCheckBox.IsChecked.GetValueOrDefault();
            Settings.Default.accurateDrumMaps = accurateDrumMapsCheckBox.IsChecked.GetValueOrDefault();

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
