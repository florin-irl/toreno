using System.Windows;

namespace Toreno;

/// <summary>
/// Interaction logic for AddUsernameWindow.xaml
/// </summary>
public partial class AddUsernameWindow : Window
{
    private readonly IReadOnlyList<string> _existingUsernames;

    public string UsernameInput { get; private set; } = "";

    public AddUsernameWindow(IReadOnlyList<string> existingUsernames)
    {
        InitializeComponent();
        _existingUsernames = existingUsernames;
        UsernameTextBox.Focus();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var input = UsernameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            ShowError("Enter a username.");
            return;
        }

        if (_existingUsernames.Any(u => string.Equals(u, input, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("That username is already being watched.");
            return;
        }

        UsernameInput = input;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
