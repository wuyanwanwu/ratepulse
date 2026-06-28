using RatePulse.Windows.Models;
using RatePulse.Windows.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    private CurrencyConversion? currentConversion;
    private TrayIconService? trayIconService;
    private bool isRefreshing;
    private bool isExitRequested;
    private bool isLoaded;
    private bool isApplyingSettings;

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
        ApplyConverterSettingsToInputs();
        UpdateFooterText();

        trayIconService = new TrayIconService(this);
        isLoaded = true;

        await LoadCachedDataAsync();
        await RefreshRatesAsync();
        ConfigureRefreshTimer();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRatesAsync();
    }

    private async void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        if (TryCaptureConverterSettings(showErrors: true))
        {
            await SaveCurrentSettingsAsync();
            await RefreshRatesAsync();
        }
    }

    private async void ConverterInput_Changed(object sender, TextChangedEventArgs e)
    {
        if (!isLoaded || isApplyingSettings)
        {
            return;
        }

        if (TryCaptureConverterSettings(showErrors: false))
        {
            await SaveCurrentSettingsAsync();
        }
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
        if (e.ButtonState == MouseButtonState.Pressed && !IsInsideInteractiveElement(e.OriginalSource as DependencyObject))
        {
            DragMove();
        }
    }

    private async Task LoadCachedDataAsync()
    {
        var cache = await rateCacheService.LoadCacheAsync();
        var hasCachedData = false;

        if (cache.Quotes.Count > 0)
        {
            SetRates(cache.Quotes);
            hasCachedData = true;
        }

        if (cache.Conversion is not null)
        {
            currentConversion = cache.Conversion;
            ShowConversion(cache.Conversion);
            hasCachedData = true;
        }
        else
        {
            ClearConversion("Enter an amount and convert via USD.");
        }

        StatusText.Text = hasCachedData
            ? "Showing cached data while refreshing..."
            : "No cached data yet.";
    }

    private async Task RefreshRatesAsync()
    {
        if (isRefreshing)
        {
            return;
        }

        if (!TryCaptureConverterSettings(showErrors: false))
        {
            return;
        }

        isRefreshing = true;
        StatusText.Text = "Refreshing...";

        try
        {
            var quotesTask = exchangeRateService.GetQuotesAsync(settings.CurrencyPairs);
            var conversionTask = exchangeRateService.ConvertViaUsdAsync(
                settings.ConverterAmount,
                settings.ConverterSourceCurrency,
                settings.ConverterTargetCurrency);

            await Task.WhenAll(quotesTask, conversionTask);

            var quotes = await quotesTask;
            currentConversion = await conversionTask;

            SetRates(quotes);
            ShowConversion(currentConversion);
            await rateCacheService.SaveAsync(quotes, currentConversion);
            StatusText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            if (Rates.Count > 0 || currentConversion is not null)
            {
                SetRates(Rates.Select(rate => rate.WithCacheState(true)));

                if (currentConversion is not null)
                {
                    currentConversion = currentConversion.WithCacheState(true);
                    ShowConversion(currentConversion);
                }

                StatusText.Text = $"Offline, showing cached data. {ex.Message}";
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

    private void ShowConversion(CurrencyConversion conversion)
    {
        ConversionSourceText.Text = conversion.SourceDisplayText;
        ConversionUsdText.Text = conversion.UsdDisplayText;
        ConversionTargetText.Text = conversion.TargetDisplayText;
        ConversionMetaText.Text = $"{conversion.RateSummaryText}  {conversion.UpdatedAtText}  {conversion.Source}  {conversion.DataStateText}";
    }

    private void ClearConversion(string message)
    {
        ConversionSourceText.Text = message;
        ConversionUsdText.Text = string.Empty;
        ConversionTargetText.Text = string.Empty;
        ConversionMetaText.Text = string.Empty;
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

    private void ApplyConverterSettingsToInputs()
    {
        isApplyingSettings = true;
        ConverterAmountTextBox.Text = settings.ConverterAmount.ToString("0.####", CultureInfo.InvariantCulture);
        ConverterSourceTextBox.Text = settings.ConverterSourceCurrency;
        ConverterTargetTextBox.Text = settings.ConverterTargetCurrency;
        isApplyingSettings = false;
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

    private bool TryCaptureConverterSettings(bool showErrors)
    {
        if (!decimal.TryParse(ConverterAmountTextBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
        {
            if (showErrors)
            {
                StatusText.Text = "Enter a valid non-negative amount.";
            }

            return false;
        }

        var sourceCurrency = ConverterSourceTextBox.Text.Trim().ToUpperInvariant();
        var targetCurrency = ConverterTargetTextBox.Text.Trim().ToUpperInvariant();

        if (sourceCurrency.Length != 3 || targetCurrency.Length != 3)
        {
            if (showErrors)
            {
                StatusText.Text = "Currency codes must be 3 letters, for example CNY or JPY.";
            }

            return false;
        }

        settings.ConverterAmount = amount;
        settings.ConverterSourceCurrency = sourceCurrency;
        settings.ConverterTargetCurrency = targetCurrency;
        return true;
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

    private static bool IsInsideInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button or System.Windows.Controls.TextBox)
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
