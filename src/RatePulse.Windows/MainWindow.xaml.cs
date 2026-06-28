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
        ApplyLanguage();
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
        settings.UiLanguage = settingsWindow.Settings.UiLanguage;
        await SaveCurrentSettingsAsync();

        ApplySettingsToWindow();
        ApplyLanguage();
        UpdateFooterText();
        RefreshDisplayLanguage();
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
            ClearConversion(Text("Enter an amount and convert via USD.", "输入金额后通过美元中转换算。"));
        }

        StatusText.Text = hasCachedData
            ? Text("Showing cached data while refreshing...", "正在显示缓存数据并刷新...")
            : Text("No cached data yet.", "暂无缓存数据。");
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
        StatusText.Text = Text("Refreshing...", "正在刷新...");

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
            StatusText.Text = Text($"Updated {DateTime.Now:HH:mm:ss}", $"已更新 {DateTime.Now:HH:mm:ss}");
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

                StatusText.Text = Text($"Offline, showing cached data. {ex.Message}", $"网络不可用，正在显示缓存数据。{ex.Message}");
            }
            else
            {
                StatusText.Text = Text($"Refresh failed: {ex.Message}", $"刷新失败：{ex.Message}");
            }
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void SetRates(IEnumerable<ExchangeRateQuote> quotes)
    {
        var quoteList = quotes.ToList();
        Rates.Clear();

        foreach (var quote in quoteList)
        {
            var displayPair = CurrencyDisplayService.PairLabel(quote.Pair, settings.UiLanguage);
            var displayState = quote.IsCached ? Text("cached", "缓存") : Text("fresh", "最新");
            Rates.Add(quote.WithDisplayText(displayPair, displayState));
        }
    }

    private void ShowConversion(CurrencyConversion conversion)
    {
        ConversionSourceText.Text = FormatCurrencyAmount(conversion.SourceAmount, conversion.SourceCurrency);
        ConversionUsdText.Text = FormatCurrencyAmount(conversion.UsdAmount, "USD");
        ConversionTargetText.Text = FormatCurrencyAmount(conversion.TargetAmount, conversion.TargetCurrency);
        ConversionMetaText.Text = FormatConversionMeta(conversion);
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
        RefreshIntervalText.Text = Text($"Auto refresh: {settings.RefreshIntervalMinutes} min", $"自动刷新：{settings.RefreshIntervalMinutes} 分钟");
        TopmostText.Text = settings.IsTopmost
            ? Text("Always on top", "窗口置顶")
            : Text("Normal window", "普通窗口");
    }

    private bool TryCaptureConverterSettings(bool showErrors)
    {
        if (!decimal.TryParse(ConverterAmountTextBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
        {
            if (showErrors)
            {
                StatusText.Text = Text("Enter a valid non-negative amount.", "请输入有效的非负金额。");
            }

            return false;
        }

        var sourceCurrency = ConverterSourceTextBox.Text.Trim().ToUpperInvariant();
        var targetCurrency = ConverterTargetTextBox.Text.Trim().ToUpperInvariant();

        if (sourceCurrency.Length != 3 || targetCurrency.Length != 3)
        {
            if (showErrors)
            {
                StatusText.Text = Text("Currency codes must be 3 letters, for example CNY or JPY.", "货币代码必须是 3 个字母，例如 CNY 或 JPY。");
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
        trayIconService?.ShowInfo("RatePulse", Text("RatePulse is still running in the tray.", "RatePulse 仍在托盘中运行。"));
    }

    private void ApplyLanguage()
    {
        SettingsButton.ToolTip = Text("Settings", "设置");
        RefreshButton.ToolTip = Text("Refresh rates and conversion", "刷新汇率和换算结果");
        MinimizeButton.ToolTip = Text("Minimize to tray", "最小化到托盘");
        CloseButton.ToolTip = Text("Exit", "退出");
        ConverterTitleText.Text = Text("USD bridge converter", "美元中转换算");
        ConvertButton.Content = Text("Convert", "换算");
        ViaUsdText.Text = Text("via USD", "经由 美元 (USD)");
        WatchlistTitleText.Text = Text("Watchlist", "关注汇率");
    }

    private void RefreshDisplayLanguage()
    {
        SetRates(Rates.ToList());

        if (currentConversion is not null)
        {
            ShowConversion(currentConversion);
        }
    }

    private string FormatCurrencyAmount(decimal amount, string currencyCode)
    {
        return $"{amount:0.####} {CurrencyDisplayService.CurrencyLabel(currencyCode, settings.UiLanguage)}";
    }

    private string FormatConversionMeta(CurrencyConversion conversion)
    {
        var sourceRateLabel = $"USD/{CurrencyDisplayService.CurrencyLabel(conversion.SourceCurrency, settings.UiLanguage)}";
        var targetRateLabel = $"USD/{CurrencyDisplayService.CurrencyLabel(conversion.TargetCurrency, settings.UiLanguage)}";
        var dataState = conversion.IsCached ? Text("cached", "缓存") : Text("fresh", "最新");
        return $"{sourceRateLabel}: {conversion.UsdToSourceRate:0.####}  {targetRateLabel}: {conversion.UsdToTargetRate:0.####}  {conversion.UpdatedAtText}  {conversion.Source}  {dataState}";
    }

    private string Text(string english, string chinese)
    {
        return CurrencyDisplayService.IsChinese(settings.UiLanguage) ? chinese : english;
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
