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
            accurateDrumMapsCheckBox.IsChecked = Settings.Default.accurateDrumMaps;
            riffmasterTiltSensitivity.Value = Settings.Default.riffmasterTiltSensitivity;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void saveButton_Click(object sender, RoutedEventArgs args)
        {
            Settings.Default.autoStart = autoStartCheckBox.IsChecked.GetValueOrDefault();
            Settings.Default.accurateDrumMaps = accurateDrumMapsCheckBox.IsChecked.GetValueOrDefault();
            Settings.Default.riffmasterTiltSensitivity = riffmasterTiltSensitivity.Value;
            
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
