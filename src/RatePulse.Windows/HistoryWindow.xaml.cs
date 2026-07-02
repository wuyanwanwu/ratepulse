using RatePulse.Windows.Models;
using RatePulse.Windows.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;
using WpfPoint = System.Windows.Point;

namespace RatePulse.Windows;

public partial class HistoryWindow : Window
{
    private readonly string pair;
    private readonly string uiLanguage;
    private RateHistory? currentHistory;

    public HistoryWindow(string pair, string uiLanguage)
    {
        InitializeComponent();

        this.pair = pair;
        this.uiLanguage = uiLanguage;
        Title = $"RatePulse - {pair}";
        HistoryTitleText.Text = FormatHistoryTitle(pair);
        HistorySubtitleText.Text = Text("15-day USD based trend", "近 15 天美元基准走势");
    }

    public void ShowLoading()
    {
        currentHistory = null;
        HistoryStatusText.Text = Text("loading...", "加载中...");
        HistoryRangeText.Text = string.Empty;
        HistoryChartCanvas.Children.Clear();
    }

    public void ShowHistory(RateHistory history)
    {
        currentHistory = history;
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

        DrawHistoryChart();
    }

    public void ShowFailure(string message, RateHistory? fallbackHistory = null)
    {
        if (fallbackHistory is not null)
        {
            ShowHistory(fallbackHistory.WithCacheState(true));
            HistoryStatusText.Text = Text("cached/offline", "缓存/离线");
            HistoryRangeText.Text = Text(
                $"Could not refresh chart: {message}",
                $"曲线刷新失败：{message}");
            return;
        }

        currentHistory = null;
        HistoryStatusText.Text = Text("failed", "失败");
        HistoryRangeText.Text = Text(
            $"Could not load chart: {message}",
            $"无法加载曲线：{message}");
        HistoryChartCanvas.Children.Clear();
    }

    private void HistoryChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawHistoryChart();
    }

    private void DrawHistoryChart()
    {
        HistoryChartCanvas.Children.Clear();
        if (currentHistory is null)
        {
            return;
        }

        var points = currentHistory.Points.OrderBy(point => point.Date).ToList();
        if (points.Count == 0)
        {
            return;
        }

        var width = HistoryChartCanvas.ActualWidth;
        var height = HistoryChartCanvas.ActualHeight;
        if (width < 160 || height < 120)
        {
            return;
        }

        const double left = 56;
        const double right = 16;
        const double top = 16;
        const double bottom = 28;

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

        for (var index = 0; index < 4; index++)
        {
            var y = top + plotHeight * index / 3;
            var rate = maxRate - (maxRate - minRate) * index / 3;

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
                Width = left - 8,
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

        for (var index = 0; index < polyline.Points.Count; index++)
        {
            var point = polyline.Points[index];
            var historyPoint = points[index];
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = lineBrush
            };
            Canvas.SetLeft(dot, point.X - 3);
            Canvas.SetTop(dot, point.Y - 3);
            HistoryChartCanvas.Children.Add(dot);

            var hitArea = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = WpfBrushes.Transparent,
                Cursor = WpfCursors.Hand,
                ToolTip = FormatPointText(historyPoint)
            };
            ToolTipService.SetInitialShowDelay(hitArea, 0);
            hitArea.MouseLeftButtonDown += (_, _) => ShowSelectedPoint(historyPoint);
            Canvas.SetLeft(hitArea, point.X - 9);
            Canvas.SetTop(hitArea, point.Y - 9);
            HistoryChartCanvas.Children.Add(hitArea);
        }

        AddChartDateLabels(points, left, plotWidth, height - bottom + 8);
    }

    private void AddChartDateLabels(IReadOnlyList<RateHistoryPoint> points, double plotLeft, double plotWidth, double top)
    {
        if (points.Count == 0)
        {
            return;
        }

        var maxLabels = Math.Clamp((int)Math.Floor(plotWidth / 76) + 1, 2, points.Count);
        var step = Math.Max(1, (int)Math.Ceiling((double)(points.Count - 1) / (maxLabels - 1)));
        var indexes = Enumerable
            .Range(0, points.Count)
            .Where(index => index % step == 0)
            .Append(points.Count - 1)
            .Distinct()
            .Order()
            .ToList();

        foreach (var index in indexes)
        {
            var x = points.Count == 1
                ? plotLeft + plotWidth
                : plotLeft + plotWidth * index / (points.Count - 1);
            AddChartDateLabel(points[index].Date, x, top);
        }
    }

    private void AddChartDateLabel(DateOnly date, double centerX, double top)
    {
        const double labelWidth = 56;
        var left = Math.Clamp(
            centerX - labelWidth / 2,
            0,
            Math.Max(0, HistoryChartCanvas.ActualWidth - labelWidth));

        var label = new TextBlock
        {
            Text = date.ToString("MM-dd", CultureInfo.InvariantCulture),
            Foreground = new SolidColorBrush(WpfColor.FromRgb(141, 150, 168)),
            FontSize = 10,
            Width = labelWidth,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        HistoryChartCanvas.Children.Add(label);
    }

    private void ShowSelectedPoint(RateHistoryPoint point)
    {
        HistoryRangeText.Text = FormatPointText(point);
    }

    private string FormatPointText(RateHistoryPoint point)
    {
        return point.Rate.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private string FormatHistoryTitle(string pair)
    {
        var parts = pair.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[0].Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return CurrencyDisplayService.PairLabel(pair, uiLanguage);
        }

        return Text(
            $"1 USD -> {CurrencyDisplayService.CurrencyLabel(parts[1], uiLanguage)}",
            $"1 美元 (USD) -> {CurrencyDisplayService.CurrencyLabel(parts[1], uiLanguage)}");
    }

    private string Text(string english, string chinese)
    {
        return CurrencyDisplayService.IsChinese(uiLanguage) ? chinese : english;
    }
}
