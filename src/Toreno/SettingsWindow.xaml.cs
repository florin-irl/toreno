using System.Windows;

namespace Toreno;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        RunAtStartupCheckBox.IsChecked = StartupManager.IsEnabled;
    }

    private void RunAtStartupCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        StartupManager.SetEnabled(RunAtStartupCheckBox.IsChecked == true);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
