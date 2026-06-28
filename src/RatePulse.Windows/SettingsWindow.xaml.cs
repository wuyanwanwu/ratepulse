using RatePulse.Windows.Models;
using System.Windows;

namespace RatePulse.Windows;

public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        Settings = settings.Clone();
        PairsTextBox.Text = string.Join(Environment.NewLine, Settings.CurrencyPairs);
        RefreshIntervalTextBox.Text = Settings.RefreshIntervalMinutes.ToString();
        TopmostCheckBox.IsChecked = Settings.IsTopmost;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var pairs = PairsTextBox.Text
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.ToUpperInvariant())
            .ToList();

        if (pairs.Count == 0 || pairs.Any(pair => pair.Split('/').Length != 2))
        {
            ValidationText.Text = "Please enter at least one valid pair, for example USD/CNY.";
            return;
        }

        if (!int.TryParse(RefreshIntervalTextBox.Text.Trim(), out var interval) || interval < 1 || interval > 1440)
        {
            ValidationText.Text = "Refresh interval must be between 1 and 1440 minutes.";
            return;
        }

        Settings.CurrencyPairs = pairs;
        Settings.RefreshIntervalMinutes = interval;
        Settings.IsTopmost = TopmostCheckBox.IsChecked == true;
        Settings.Normalize();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
