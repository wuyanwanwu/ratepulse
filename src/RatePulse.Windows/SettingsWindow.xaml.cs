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
        LanguageComboBox.SelectedValue = Settings.UiLanguage;
        ApplyLanguage();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var pairs = PairsTextBox.Text
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.ToUpperInvariant())
            .ToList();

        if (pairs.Count == 0 || pairs.Any(pair => pair.Split('/').Length != 2))
        {
            ValidationText.Text = Text("Please enter at least one valid pair, for example USD/CNY.", "请输入至少一个有效货币对，例如 USD/CNY。");
            return;
        }

        if (!int.TryParse(RefreshIntervalTextBox.Text.Trim(), out var interval) || interval < 1 || interval > 1440)
        {
            ValidationText.Text = Text("Refresh interval must be between 1 and 1440 minutes.", "刷新间隔必须在 1 到 1440 分钟之间。");
            return;
        }

        Settings.CurrencyPairs = pairs;
        Settings.RefreshIntervalMinutes = interval;
        Settings.IsTopmost = TopmostCheckBox.IsChecked == true;
        Settings.UiLanguage = LanguageComboBox.SelectedValue?.ToString() == "zh" ? "zh" : "en";
        Settings.Normalize();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        var isChinese = LanguageComboBox.SelectedValue?.ToString() == "zh";

        Title = isChinese ? "RatePulse 设置" : "RatePulse Settings";
        SettingsTitleText.Text = isChinese ? "设置" : "Settings";
        LanguageLabel.Text = isChinese ? "界面语言" : "Language";
        CurrencyPairsLabel.Text = isChinese ? "关注汇率" : "Currency pairs";
        CurrencyPairsHint.Text = isChinese ? "每行一个货币对，例如 USD/CNY。" : "One pair per line, like USD/CNY.";
        RefreshIntervalLabel.Text = isChinese ? "刷新间隔（分钟）" : "Refresh interval (minutes)";
        TopmostCheckBox.Content = isChinese ? "窗口置顶" : "Always on top";
        CancelButton.Content = isChinese ? "取消" : "Cancel";
        SaveButton.Content = isChinese ? "保存" : "Save";
    }

    private string Text(string english, string chinese)
    {
        return LanguageComboBox.SelectedValue?.ToString() == "zh" ? chinese : english;
    }
}
