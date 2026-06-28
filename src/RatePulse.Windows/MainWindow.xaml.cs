using RatePulse.Windows.Models;
using RatePulse.Windows.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RatePulse.Windows;

public partial class MainWindow : Window
{
    private readonly ExchangeRateService exchangeRateService = new();
    private readonly SettingsService settingsService = new();
    private readonly RateCacheService rateCacheService = new();
    private readonly DispatcherTimer refreshTimer = new();

    private AppSettings settings = new();
    private TrayIconService? trayIconService;
    private bool isRefreshing;
    private bool isExitRequested;
    private bool isLoaded;

    public ObservableCollection<ExchangeRateQuote> Rates { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        refreshTimer.Tick += async (_, _) => await RefreshRatesAsync();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        settings = await settingsService.LoadAsync();
        ApplySettingsToWindow();
        UpdateFooterText();

        trayIconService = new TrayIconService(this);
        isLoaded = true;

        await LoadCachedRatesAsync();
        await RefreshRatesAsync();
        ConfigureRefreshTimer();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRatesAsync();
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(settings)
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() != true)
        {
            return;
        }

        settings.CurrencyPairs = settingsWindow.Settings.CurrencyPairs;
        settings.RefreshIntervalMinutes = settingsWindow.Settings.RefreshIntervalMinutes;
        settings.IsTopmost = settingsWindow.Settings.IsTopmost;
        await SaveCurrentSettingsAsync();

        ApplySettingsToWindow();
        UpdateFooterText();
        ConfigureRefreshTimer();
        await RefreshRatesAsync();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        isExitRequested = true;
        Close();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!isExitRequested)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        refreshTimer.Stop();
        trayIconService?.Dispose();
        SaveCurrentSettingsAsync().GetAwaiter().GetResult();
    }

    private async void Window_PlacementChanged(object sender, EventArgs e)
    {
        if (!isLoaded || WindowState != WindowState.Normal)
        {
            return;
        }

        CaptureWindowPlacement();
        await SaveCurrentSettingsAsync();
    }

    private void WindowChrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !IsInsideButton(e.OriginalSource as DependencyObject))
        {
            DragMove();
        }
    }

    private async Task LoadCachedRatesAsync()
    {
        var cachedQuotes = await rateCacheService.LoadAsync();
        if (cachedQuotes.Count == 0)
        {
            StatusText.Text = "No cached rates yet.";
            return;
        }

        SetRates(cachedQuotes);
        StatusText.Text = "Showing cached rates while refreshing...";
    }

    private async Task RefreshRatesAsync()
    {
        if (isRefreshing)
        {
            return;
        }

        isRefreshing = true;
        StatusText.Text = "Refreshing...";

        try
        {
            var quotes = await exchangeRateService.GetQuotesAsync(settings.CurrencyPairs);
            SetRates(quotes);
            await rateCacheService.SaveAsync(quotes);
            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            if (Rates.Count > 0)
            {
                SetRates(Rates.Select(rate => rate.WithCacheState(true)));
                StatusText.Text = $"Offline, showing cached rates. {ex.Message}";
            }
            else
            {
                StatusText.Text = $"Refresh failed: {ex.Message}";
            }
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void SetRates(IEnumerable<ExchangeRateQuote> quotes)
    {
        Rates.Clear();

        foreach (var quote in quotes)
        {
            Rates.Add(quote);
        }
    }

    private void ApplySettingsToWindow()
    {
        Topmost = settings.IsTopmost;
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        if (IsValidWindowCoordinate(settings.WindowLeft) && IsValidWindowCoordinate(settings.WindowTop))
        {
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
        }

        ConfigureRefreshTimer();
    }

    private void ConfigureRefreshTimer()
    {
        refreshTimer.Stop();
        refreshTimer.Interval = TimeSpan.FromMinutes(settings.RefreshIntervalMinutes);
        refreshTimer.Start();
    }

    private void UpdateFooterText()
    {
        RefreshIntervalText.Text = $"Auto refresh: {settings.RefreshIntervalMinutes} min";
        TopmostText.Text = settings.IsTopmost ? "Always on top" : "Normal window";
    }

    private void CaptureWindowPlacement()
    {
        settings.WindowLeft = Left;
        settings.WindowTop = Top;
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
    }

    private async Task SaveCurrentSettingsAsync()
    {
        CaptureWindowPlacement();
        await settingsService.SaveAsync(settings);
    }

    private void HideToTray()
    {
        Hide();
        WindowState = WindowState.Normal;
        trayIconService?.ShowInfo("RatePulse", "RatePulse is still running in the tray.");
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static bool IsValidWindowCoordinate(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
