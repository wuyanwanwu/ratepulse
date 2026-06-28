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

    private static readonly Dictionary<string, string> ChineseNameOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TRY"] = "\u571f\u8033\u5176\u91cc\u62c9",
        ["AED"] = "\u963f\u8054\u914b\u8fea\u62c9\u59c6",
        ["BRL"] = "\u5df4\u897f\u96f7\u4e9a\u5c14",
        ["DKK"] = "\u4e39\u9ea6\u514b\u6717",
        ["MXN"] = "\u58a8\u897f\u54e5\u6bd4\u7d22",
        ["NOK"] = "\u632a\u5a01\u514b\u6717",
        ["PLN"] = "\u6ce2\u5170\u5179\u7f57\u63d0",
        ["RUB"] = "\u4fc4\u7f57\u65af\u5362\u5e03",
        ["SAR"] = "\u6c99\u7279\u91cc\u4e9a\u5c14",
        ["SEK"] = "\u745e\u5178\u514b\u6717",
        ["TWD"] = "\u65b0\u53f0\u5e01",
        ["ZAR"] = "\u5357\u975e\u5170\u7279"
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

        return ChineseNameOverrides.TryGetValue(normalized, out var overrideName)
            ? $"{overrideName} ({normalized})"
            : ChineseNames.TryGetValue(normalized, out var chineseName)
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
