using System;
using System.Windows;
using System.Windows.Input;
using RB4InstrumentMapper.Core;
using RB4InstrumentMapper.GUI.Properties;

namespace RB4InstrumentMapper.GUI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private uint _pollingFrequency;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            autoStartCheckBox.IsChecked = Settings.Default.autoStart;
            accurateDrumMapsCheckBox.IsChecked = Settings.Default.accurateDrumMaps;
            _pollingFrequency = BackendSettings.ClampPollingFrequency(Settings.Default.pollingFrequency);

            pollingFrequencyTextBox.Text = _pollingFrequency.ToString();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs args)
        {
            // Workaround for textboxes not getting unfocused when clicking the window,
            // they only get unfocused when clicking another element
            if (args.MouseDevice.DirectlyOver != Keyboard.FocusedElement)
            {
                Keyboard.ClearFocus();
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs args)
        {
            // Changes to settings are only saved when hitting Save
            Settings.Default.autoStart = autoStartCheckBox.IsChecked.GetValueOrDefault();
            Settings.Default.accurateDrumMaps = accurateDrumMapsCheckBox.IsChecked.GetValueOrDefault();
            Settings.Default.pollingFrequency = _pollingFrequency;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }

        private void pollingFrequencyTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!uint.TryParse(pollingFrequencyTextBox.Text, out uint value))
            {
                value = _pollingFrequency;
            }

            _pollingFrequency = BackendSettings.ClampPollingFrequency(value);
            pollingFrequencyTextBox.Text = _pollingFrequency.ToString();
        }
    }
}
