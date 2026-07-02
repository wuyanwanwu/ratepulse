using RatePulse.Windows.Models;
using RatePulse.Windows.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfPoint = System.Windows.Point;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace RatePulse.Windows;

public partial class MainWindow : Window
{
    private const int HistoryDays = 15;

    private readonly ExchangeRateService exchangeRateService = new();
    private readonly SettingsService settingsService = new();
    private readonly RateCacheService rateCacheService = new();
    private readonly DispatcherTimer refreshTimer = new();
    private readonly DispatcherTimer placementSaveTimer = new();
    private readonly Dictionary<string, RateHistory> historiesByPair = new(StringComparer.OrdinalIgnoreCase);

    private AppSettings settings = new();
    private CurrencyConversion? currentConversion;
    private RateHistory? currentHistory;
    private TrayIconService? trayIconService;
    private CancellationTokenSource? historyCancellation;
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
        placementSaveTimer.Interval = TimeSpan.FromMilliseconds(600);
        placementSaveTimer.Tick += async (_, _) =>
        {
            placementSaveTimer.Stop();
            await SaveCurrentSettingsAsync();
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        settings = await settingsService.LoadAsync();
        ApplySettingsToWindow();
        ApplyLanguage();
        PopulateCurrencyComboBoxes();
        ApplyConverterSettingsToInputs();
        UpdateFooterText();

        trayIconService = new TrayIconService(this, RequestExit);
        isLoaded = true;

        await LoadCachedDataAsync();
        await SaveCurrentSettingsAsync();
        ConfigureRefreshTimer();
        _ = RefreshRatesAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRatesAsync();
    }

    private async void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        settings.UiLanguage = CurrencyDisplayService.IsChinese(settings.UiLanguage) ? "en" : "zh";
        await SaveCurrentSettingsAsync();

        ApplyLanguage();
        PopulateCurrencyComboBoxes();
        ApplyConverterSettingsToInputs();
        UpdateFooterText();
        RefreshDisplayLanguage();
        StatusText.Text = CurrencyDisplayService.IsChinese(settings.UiLanguage)
            ? "已切换为中文。"
            : "Language switched to English.";
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

    private async void ConverterCurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isLoaded || isApplyingSettings)
        {
            return;
        }

        if (TryCaptureConverterSettings(showErrors: false))
        {
            await SaveCurrentSettingsAsync();
            await RefreshRatesAsync();
        }
    }

    private async void ConverterCurrencyComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!isLoaded || isApplyingSettings)
        {
            return;
        }

        if (TryCaptureConverterSettings(showErrors: e.Key == Key.Enter))
        {
            await SaveCurrentSettingsAsync();

            if (e.Key == Key.Enter)
            {
                await RefreshRatesAsync();
            }
        }
    }

    private async void ConverterCurrencyComboBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
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
        settings.Normalize();
        await SaveCurrentSettingsAsync();

        ApplySettingsToWindow();
        ApplyLanguage();
        PopulateCurrencyComboBoxes();
        ApplyConverterSettingsToInputs();
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
        RequestExit();
    }

    private async void RateItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as WpfButton)?.CommandParameter is ExchangeRateQuote quote)
        {
            await ShowHistoryForQuoteAsync(quote);
        }
    }

    private void HistoryChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawHistoryChart(currentHistory);
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
        placementSaveTimer.Stop();
        historyCancellation?.Cancel();
        trayIconService?.Dispose();
        CaptureWindowPlacement();
        settingsService.Save(settings);
    }

    private void Window_PlacementChanged(object sender, EventArgs e)
    {
        if (!isLoaded || !IsVisible || WindowState != WindowState.Normal)
        {
            return;
        }

        CaptureWindowPlacement();
        placementSaveTimer.Stop();
        placementSaveTimer.Start();
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

        historiesByPair.Clear();
        foreach (var history in cache.Histories)
        {
            historiesByPair[history.Pair] = history;
        }

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

        var canConvert = TryCaptureConverterSettings(showErrors: false);
        isRefreshing = true;
        StatusText.Text = Text("Refreshing...", "正在刷新...");

        IReadOnlyList<ExchangeRateQuote>? quotes = null;
        CurrencyConversion? conversion = null;
        Exception? refreshError = null;

        try
        {
            quotes = await exchangeRateService.GetQuotesAsync(settings.CurrencyPairs);

            if (canConvert)
            {
                conversion = await exchangeRateService.ConvertViaUsdAsync(
                    settings.ConverterAmount,
                    settings.ConverterSourceCurrency,
                    settings.ConverterTargetCurrency);
            }
        }
        catch (Exception ex)
        {
            refreshError = ex;
        }

        try
        {
            if (quotes is not null)
            {
                SetRates(quotes);
            }

            if (conversion is not null)
            {
                currentConversion = conversion;
                ShowConversion(conversion);
            }

            if (quotes is not null || conversion is not null)
            {
                await rateCacheService.SaveAsync(Rates, currentConversion, historiesByPair.Values);
                StatusText.Text = Text($"Updated {DateTime.Now:HH:mm:ss}", $"已更新 {DateTime.Now:HH:mm:ss}");
                return;
            }

            throw refreshError ?? new InvalidOperationException("Refresh failed.");
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

                StatusText.Text = Text(
                    $"Offline, showing cached data. {ex.Message}",
                    $"网络不可用，正在显示缓存数据。{ex.Message}");
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

    private async Task ShowHistoryForQuoteAsync(ExchangeRateQuote quote)
    {
        var quoteCurrency = GetUsdQuoteCurrency(quote.Pair);
        if (string.IsNullOrWhiteSpace(quoteCurrency))
        {
            StatusText.Text = Text("History is available for USD-based pairs only.", "历史曲线仅支持 USD 基准的汇率。");
            return;
        }

        var pair = $"USD/{quoteCurrency}";
        HistoryPanel.Visibility = Visibility.Visible;

        if (historiesByPair.TryGetValue(pair, out var cachedHistory))
        {
            ShowHistory(cachedHistory);
        }
        else
        {
            ShowHistoryLoading(pair);
        }

        historyCancellation?.Cancel();
        historyCancellation = new CancellationTokenSource();

        try
        {
            var freshHistory = await exchangeRateService.GetUsdHistoryAsync(
                quoteCurrency,
                HistoryDays,
                historyCancellation.Token);

            historiesByPair[pair] = freshHistory;
            ShowHistory(freshHistory);
            await rateCacheService.SaveAsync(Rates, currentConversion, historiesByPair.Values);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (historiesByPair.TryGetValue(pair, out var fallbackHistory))
            {
                ShowHistory(fallbackHistory.WithCacheState(true));
                HistoryStatusText.Text = Text("cached/offline", "缓存/离线");
                HistoryRangeText.Text = Text(
                    $"Could not refresh chart: {ex.Message}",
                    $"曲线刷新失败：{ex.Message}");
            }
            else
            {
                HistoryStatusText.Text = Text("failed", "失败");
                HistoryRangeText.Text = Text(
                    $"Could not load chart: {ex.Message}",
                    $"无法加载曲线：{ex.Message}");
                HistoryChartCanvas.Children.Clear();
            }
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

    private void ShowHistoryLoading(string pair)
    {
        currentHistory = null;
        HistoryTitleText.Text = FormatHistoryTitle(pair);
        HistoryStatusText.Text = Text("loading...", "加载中...");
        HistoryRangeText.Text = string.Empty;
        HistoryChartCanvas.Children.Clear();
    }

    private void ShowHistory(RateHistory history)
    {
        currentHistory = history;
        HistoryTitleText.Text = FormatHistoryTitle(history.Pair);
        HistoryStatusText.Text = history.IsCached
            ? Text("cached", "缓存")
            : Text("fresh", "最新");

        var orderedPoints = history.Points.OrderBy(point => point.Date).ToList();
        if (orderedPoints.Count > 0)
        {
            var first = orderedPoints[0];
            var last = orderedPoints[^1];
            HistoryRangeText.Text = Text(
                $"{first.Date:MM-dd} to {last.Date:MM-dd} · latest {last.Rate:0.####} · {history.Source}",
                $"{first.Date:MM-dd} 至 {last.Date:MM-dd} · 最新 {last.Rate:0.####} · {history.Source}");
        }
        else
        {
            HistoryRangeText.Text = Text("No chart points.", "暂无曲线点位。");
        }

        DrawHistoryChart(history);
    }

    private void DrawHistoryChart(RateHistory? history)
    {
        HistoryChartCanvas.Children.Clear();
        if (history is null)
        {
            return;
        }

        var points = history.Points.OrderBy(point => point.Date).ToList();
        if (points.Count == 0)
        {
            return;
        }

        var width = HistoryChartCanvas.ActualWidth;
        var height = HistoryChartCanvas.ActualHeight;
        if (width < 120 || height < 80)
        {
            return;
        }

        const double left = 48;
        const double right = 12;
        const double top = 12;
        const double bottom = 24;

        var plotWidth = Math.Max(1, width - left - right);
        var plotHeight = Math.Max(1, height - top - bottom);
        var minRate = points.Min(point => point.Rate);
        var maxRate = points.Max(point => point.Rate);

        if (minRate == maxRate)
        {
            minRate -= 0.01m;
            maxRate += 0.01m;
        }

        var gridBrush = new SolidColorBrush(WpfColor.FromRgb(48, 56, 75));
        var labelBrush = new SolidColorBrush(WpfColor.FromRgb(141, 150, 168));
        var lineBrush = new SolidColorBrush(WpfColor.FromRgb(123, 227, 162));

        for (var index = 0; index < 3; index++)
        {
            var y = top + plotHeight * index / 2;
            var rate = maxRate - (maxRate - minRate) * index / 2;

            HistoryChartCanvas.Children.Add(new Line
            {
                X1 = left,
                Y1 = y,
                X2 = width - right,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1
            });

            var label = new TextBlock
            {
                Text = rate.ToString("0.####"),
                Foreground = labelBrush,
                FontSize = 10,
                Width = left - 6,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, Math.Max(0, y - 8));
            HistoryChartCanvas.Children.Add(label);
        }

        var polyline = new Polyline
        {
            Stroke = lineBrush,
            StrokeThickness = 2
        };

        for (var index = 0; index < points.Count; index++)
        {
            var x = points.Count == 1
                ? left + plotWidth
                : left + plotWidth * index / (points.Count - 1);
            var rateOffset = (double)((maxRate - points[index].Rate) / (maxRate - minRate));
            var y = top + plotHeight * rateOffset;
            polyline.Points.Add(new WpfPoint(x, y));
        }

        HistoryChartCanvas.Children.Add(polyline);

        var lastPoint = polyline.Points[^1];
        var dot = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = lineBrush
        };
        Canvas.SetLeft(dot, lastPoint.X - 3.5);
        Canvas.SetTop(dot, lastPoint.Y - 3.5);
        HistoryChartCanvas.Children.Add(dot);

        AddChartDateLabel(points[0].Date, left, height - bottom + 6, TextAlignment.Left);
        AddChartDateLabel(points[^1].Date, width - right - 56, height - bottom + 6, TextAlignment.Right);
    }

    private void AddChartDateLabel(DateOnly date, double left, double top, TextAlignment alignment)
    {
        var label = new TextBlock
        {
            Text = date.ToString("MM-dd", CultureInfo.InvariantCulture),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(141, 150, 168)),
            FontSize = 10,
            Width = 56,
            TextAlignment = alignment
        };

        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        HistoryChartCanvas.Children.Add(label);
    }

    private void ApplySettingsToWindow()
    {
        Topmost = settings.IsTopmost;
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        if (IsUsableWindowPlacement(settings.WindowLeft, settings.WindowTop, settings.WindowWidth, settings.WindowHeight))
        {
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
        }
    }

    private void PopulateCurrencyComboBoxes()
    {
        isApplyingSettings = true;
        var options = CurrencyDisplayService.CurrencyOptions(settings.UiLanguage);
        ConverterSourceComboBox.ItemsSource = options;
        ConverterTargetComboBox.ItemsSource = options;
        isApplyingSettings = false;
    }

    private void ApplyConverterSettingsToInputs()
    {
        isApplyingSettings = true;
        ConverterAmountTextBox.Text = settings.ConverterAmount.ToString("0.####", CultureInfo.InvariantCulture);
        ConverterSourceComboBox.Text = CurrencyDisplayService.CurrencyLabel(settings.ConverterSourceCurrency, settings.UiLanguage);
        ConverterTargetComboBox.Text = CurrencyDisplayService.CurrencyLabel(settings.ConverterTargetCurrency, settings.UiLanguage);
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

        var sourceCurrency = CurrencyDisplayService.ExtractCurrencyCode(ConverterSourceComboBox.Text);
        var targetCurrency = CurrencyDisplayService.ExtractCurrencyCode(ConverterTargetComboBox.Text);

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
        if (IsUsableWindowPlacement(Left, Top, Width, Height))
        {
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
        }

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
        if (isExitRequested)
        {
            return;
        }

        Hide();
        WindowState = WindowState.Normal;
        trayIconService?.ShowInfo("RatePulse", Text("RatePulse is still running in the tray.", "RatePulse 仍在托盘中运行。"));
    }

    private void RequestExit()
    {
        if (isExitRequested)
        {
            return;
        }

        isExitRequested = true;
        refreshTimer.Stop();
        placementSaveTimer.Stop();
        historyCancellation?.Cancel();
        Close();
    }

    private void ApplyLanguage()
    {
        LanguageButton.Content = CurrencyDisplayService.IsChinese(settings.UiLanguage) ? "EN" : "ZH";
        LanguageButton.ToolTip = Text("Switch to Chinese", "切换到英文");
        SettingsButton.ToolTip = Text("Settings", "设置");
        RefreshButton.ToolTip = Text("Refresh rates and conversion", "刷新汇率和换算结果");
        MinimizeButton.ToolTip = Text("Minimize to tray", "最小化到托盘");
        CloseButton.ToolTip = Text("Exit", "退出");
        ConverterTitleText.Text = Text("USD bridge converter", "美元中转换算");
        ConvertButton.Content = Text("Convert", "换算");
        ViaUsdText.Text = Text("via USD", "经由 美元 (USD)");
        ViaUsdInlineText.Text = Text("-> USD ->", "-> USD ->");
        WatchlistTitleText.Text = Text("Watchlist", "关注汇率");
    }

    private void RefreshDisplayLanguage()
    {
        SetRates(Rates.ToList());

        if (currentConversion is not null)
        {
            ShowConversion(currentConversion);
        }

        if (currentHistory is not null)
        {
            ShowHistory(currentHistory);
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

    private string FormatHistoryTitle(string pair)
    {
        var quoteCurrency = GetUsdQuoteCurrency(pair);
        return string.IsNullOrWhiteSpace(quoteCurrency)
            ? CurrencyDisplayService.PairLabel(pair, settings.UiLanguage)
            : Text(
                $"1 USD -> {CurrencyDisplayService.CurrencyLabel(quoteCurrency, settings.UiLanguage)}",
                $"1 美元 (USD) -> {CurrencyDisplayService.CurrencyLabel(quoteCurrency, settings.UiLanguage)}");
    }

    private string Text(string english, string chinese)
    {
        return CurrencyDisplayService.IsChinese(settings.UiLanguage) ? chinese : english;
    }

    private static string GetUsdQuoteCurrency(string pair)
    {
        var parts = pair.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return string.Empty;
        }

        if (parts[0].Equals("USD", StringComparison.OrdinalIgnoreCase) &&
            !parts[1].Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return parts[1].ToUpperInvariant();
        }

        if (parts[1].Equals("USD", StringComparison.OrdinalIgnoreCase) &&
            !parts[0].Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return parts[0].ToUpperInvariant();
        }

        return string.Empty;
    }

    private static bool IsInsideInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is WpfButton or WpfTextBox or WpfComboBox)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static bool IsUsableWindowPlacement(double left, double top, double width, double height)
    {
        if (double.IsNaN(left) || double.IsInfinity(left) ||
            double.IsNaN(top) || double.IsInfinity(top) ||
            double.IsNaN(width) || double.IsInfinity(width) ||
            double.IsNaN(height) || double.IsInfinity(height))
        {
            return false;
        }

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        return left + width > virtualLeft &&
            left < virtualRight &&
            top + height > virtualTop &&
            top < virtualBottom;
    }
}
