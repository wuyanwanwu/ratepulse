using RatePulse.Windows.Models;
using RatePulse.Windows.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RatePulse.Windows;

public partial class MainWindow : Window
{
    private static readonly string[] DefaultPairs =
    [
        "USD/CNY",
        "EUR/CNY",
        "JPY/CNY",
        "HKD/CNY",
        "GBP/CNY"
    ];

    private readonly ExchangeRateService exchangeRateService = new();
    private readonly DispatcherTimer refreshTimer = new();

    public ObservableCollection<ExchangeRateQuote> Rates { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        refreshTimer.Interval = TimeSpan.FromMinutes(5);
        refreshTimer.Tick += async (_, _) => await RefreshRatesAsync();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshRatesAsync();
        refreshTimer.Start();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRatesAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowChrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !IsInsideButton(e.OriginalSource as DependencyObject))
        {
            DragMove();
        }
    }

    private async Task RefreshRatesAsync()
    {
        StatusText.Text = "Refreshing...";

        try
        {
            var quotes = await exchangeRateService.GetQuotesAsync(DefaultPairs);
            Rates.Clear();

            foreach (var quote in quotes)
            {
                Rates.Add(quote);
            }

            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Refresh failed: {ex.Message}";
        }
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
