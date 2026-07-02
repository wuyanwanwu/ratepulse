using System.Text.RegularExpressions;

namespace RatePulse.Windows.Models;

public sealed class AppSettings
{
    private const int CurrentSettingsVersion = 2;
    private static readonly Regex CurrencyCodeRegex = new("[A-Z]{3}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public int SettingsVersion { get; set; }

    public List<string> CurrencyPairs { get; set; } =
    [
        "USD/CNY",
        "USD/EUR",
        "USD/JPY",
        "USD/HKD",
        "USD/GBP",
        "USD/AUD",
        "USD/CAD",
        "USD/CHF",
        "USD/SGD",
        "USD/TRY"
    ];

    public int RefreshIntervalMinutes { get; set; } = 5;

    public string UiLanguage { get; set; } = "en";

    public decimal ConverterAmount { get; set; } = 1000m;

    public string ConverterSourceCurrency { get; set; } = "CNY";

    public string ConverterTargetCurrency { get; set; } = "USD";

    public bool IsTopmost { get; set; } = true;

    public double WindowLeft { get; set; } = 80;

    public double WindowTop { get; set; } = 80;

    public double WindowWidth { get; set; } = 380;

    public double WindowHeight { get; set; } = 680;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CurrencyPairs = [.. CurrencyPairs],
            SettingsVersion = SettingsVersion,
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
            .Select(NormalizeUsdWatchPair)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (CurrencyPairs.Count == 0)
        {
            CurrencyPairs = ["USD/CNY"];
        }

        if (SettingsVersion < CurrentSettingsVersion)
        {
            ConverterSourceCurrency = "CNY";
            ConverterTargetCurrency = "USD";
            CurrencyPairs.RemoveAll(pair => pair.Equals("USD/CNY", StringComparison.OrdinalIgnoreCase));
            CurrencyPairs.Insert(0, "USD/CNY");
            SettingsVersion = CurrentSettingsVersion;
        }

        RefreshIntervalMinutes = Math.Clamp(RefreshIntervalMinutes, 1, 1440);
        UiLanguage = UiLanguage.Equals("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
        ConverterAmount = Math.Max(0, ConverterAmount);
        ConverterSourceCurrency = NormalizeCurrencyCode(ConverterSourceCurrency, "CNY");
        ConverterTargetCurrency = NormalizeCurrencyCode(ConverterTargetCurrency, "USD");
        WindowLeft = NormalizeWindowCoordinate(WindowLeft);
        WindowTop = NormalizeWindowCoordinate(WindowTop);
        WindowWidth = Math.Clamp(WindowWidth, 360, 1000);
        WindowHeight = Math.Clamp(WindowHeight, 620, 1200);
    }

    private static string NormalizeCurrencyCode(string? currencyCode, string fallback)
    {
        var normalized = ExtractCurrencyCode(currencyCode);
        return normalized is { Length: 3 } ? normalized : fallback;
    }

    private static string? NormalizeUsdWatchPair(string? pair)
    {
        var normalized = pair?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var parts = normalized.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            var first = ExtractCurrencyCode(parts[0]);
            var second = ExtractCurrencyCode(parts[1]);

            if (first is not { Length: 3 } || second is not { Length: 3 })
            {
                return null;
            }

            if (first.Equals("USD", StringComparison.OrdinalIgnoreCase) && !second.Equals("USD", StringComparison.OrdinalIgnoreCase))
            {
                return $"USD/{second}";
            }

            if (second.Equals("USD", StringComparison.OrdinalIgnoreCase) && !first.Equals("USD", StringComparison.OrdinalIgnoreCase))
            {
                return $"USD/{first}";
            }

            return first.Equals("USD", StringComparison.OrdinalIgnoreCase)
                ? null
                : $"USD/{first}";
        }

        var currencyCode = ExtractCurrencyCode(normalized);
        return currencyCode is { Length: 3 } && !currencyCode.Equals("USD", StringComparison.OrdinalIgnoreCase)
            ? $"USD/{currencyCode}"
            : null;
    }

    private static string? ExtractCurrencyCode(string? value)
    {
        var match = CurrencyCodeRegex.Match(value ?? string.Empty);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static double NormalizeWindowCoordinate(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 10000
            ? 80
            : value;
    }
}
