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

    public string UiLanguage { get; set; } = "en";

    public decimal ConverterAmount { get; set; } = 1000m;

    public string ConverterSourceCurrency { get; set; } = "CNY";

    public string ConverterTargetCurrency { get; set; } = "JPY";

    public bool IsTopmost { get; set; } = true;

    public double WindowLeft { get; set; } = double.NaN;

    public double WindowTop { get; set; } = double.NaN;

    public double WindowWidth { get; set; } = 340;

    public double WindowHeight { get; set; } = 560;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CurrencyPairs = [.. CurrencyPairs],
            RefreshIntervalMinutes = RefreshIntervalMinutes,
            UiLanguage = UiLanguage,
            ConverterAmount = ConverterAmount,
            ConverterSourceCurrency = ConverterSourceCurrency,
            ConverterTargetCurrency = ConverterTargetCurrency,
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
        UiLanguage = UiLanguage.Equals("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
        ConverterAmount = Math.Max(0, ConverterAmount);
        ConverterSourceCurrency = NormalizeCurrencyCode(ConverterSourceCurrency, "CNY");
        ConverterTargetCurrency = NormalizeCurrencyCode(ConverterTargetCurrency, "JPY");
        WindowWidth = Math.Clamp(WindowWidth, 320, 1000);
        WindowHeight = Math.Clamp(WindowHeight, 500, 1200);
    }

    private static string NormalizeCurrencyCode(string? currencyCode, string fallback)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } ? normalized : fallback;
    }
}
