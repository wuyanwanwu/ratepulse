namespace RatePulse.Windows.Models;

public sealed class AppSettings
{
    public List<string> CurrencyPairs { get; set; } =
    [
        "USD/CNY",
        "EUR/CNY",
        "JPY/CNY",
        "HKD/CNY",
        "GBP/CNY"
    ];

    public int RefreshIntervalMinutes { get; set; } = 5;

    public bool IsTopmost { get; set; } = true;

    public double WindowLeft { get; set; } = double.NaN;

    public double WindowTop { get; set; } = double.NaN;

    public double WindowWidth { get; set; } = 300;

    public double WindowHeight { get; set; } = 360;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CurrencyPairs = [.. CurrencyPairs],
            RefreshIntervalMinutes = RefreshIntervalMinutes,
            IsTopmost = IsTopmost,
            WindowLeft = WindowLeft,
            WindowTop = WindowTop,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight
        };
    }

    public void Normalize()
    {
        CurrencyPairs = CurrencyPairs
            .Select(pair => pair.Trim().ToUpperInvariant())
            .Where(pair => !string.IsNullOrWhiteSpace(pair))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (CurrencyPairs.Count == 0)
        {
            CurrencyPairs = ["USD/CNY"];
        }

        RefreshIntervalMinutes = Math.Clamp(RefreshIntervalMinutes, 1, 1440);
        WindowWidth = Math.Clamp(WindowWidth, 280, 1000);
        WindowHeight = Math.Clamp(WindowHeight, 320, 1200);
    }
}
