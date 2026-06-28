namespace RatePulse.Windows.Services;

public static class CurrencyDisplayService
{
    private static readonly Dictionary<string, string> ChineseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "美元",
        ["CNY"] = "人民币",
        ["JPY"] = "日元",
        ["EUR"] = "欧元",
        ["HKD"] = "港元",
        ["GBP"] = "英镑",
        ["AUD"] = "澳元",
        ["CAD"] = "加元",
        ["CHF"] = "瑞士法郎",
        ["SGD"] = "新加坡元",
        ["KRW"] = "韩元",
        ["THB"] = "泰铢",
        ["MYR"] = "马来西亚林吉特",
        ["VND"] = "越南盾",
        ["IDR"] = "印尼盾",
        ["PHP"] = "菲律宾比索",
        ["INR"] = "印度卢比",
        ["NZD"] = "新西兰元"
    };

    public static bool IsChinese(string language)
    {
        return language.Equals("zh", StringComparison.OrdinalIgnoreCase);
    }

    public static string CurrencyLabel(string currencyCode, string language)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (!IsChinese(language))
        {
            return normalized;
        }

        return ChineseNames.TryGetValue(normalized, out var chineseName)
            ? $"{chineseName} ({normalized})"
            : normalized;
    }

    public static string PairLabel(string pair, string language)
    {
        var parts = pair.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return pair;
        }

        return $"{CurrencyLabel(parts[0], language)} / {CurrencyLabel(parts[1], language)}";
    }
}
