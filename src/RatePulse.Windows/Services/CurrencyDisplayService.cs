namespace RatePulse.Windows.Services;

public static class CurrencyDisplayService
{
    public static readonly IReadOnlyList<string> CommonCurrencyCodes =
    [
        "USD",
        "CNY",
        "EUR",
        "JPY",
        "HKD",
        "GBP",
        "AUD",
        "CAD",
        "CHF",
        "SGD",
        "KRW",
        "TRY",
        "THB",
        "MYR",
        "VND",
        "IDR",
        "PHP",
        "INR",
        "NZD",
        "TWD",
        "AED",
        "BRL",
        "MXN",
        "SEK",
        "NOK",
        "DKK",
        "PLN",
        "ZAR"
    ];

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
        ["NZD"] = "新西兰元",
        ["TRY"] = "土耳其里拉",
        ["AED"] = "阿联酋迪拉姆",
        ["BRL"] = "巴西雷亚尔",
        ["DKK"] = "丹麦克朗",
        ["MXN"] = "墨西哥比索",
        ["NOK"] = "挪威克朗",
        ["PLN"] = "波兰兹罗提",
        ["RUB"] = "俄罗斯卢布",
        ["SAR"] = "沙特里亚尔",
        ["SEK"] = "瑞典克朗",
        ["TWD"] = "新台币",
        ["ZAR"] = "南非兰特"
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

    public static IReadOnlyList<string> CurrencyOptions(string language, bool includeUsd = true)
    {
        return CommonCurrencyCodes
            .Where(code => includeUsd || !code.Equals("USD", StringComparison.OrdinalIgnoreCase))
            .Select(code => CurrencyLabel(code, language))
            .ToList();
    }

    public static string ExtractCurrencyCode(string input)
    {
        var normalized = input.Trim().ToUpperInvariant();
        var start = -1;

        for (var i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] is >= 'A' and <= 'Z')
            {
                start = i;
                break;
            }
        }

        if (start < 0)
        {
            return string.Empty;
        }

        var code = new string(normalized
            .Skip(start)
            .TakeWhile(character => character is >= 'A' and <= 'Z')
            .Take(3)
            .ToArray());

        return code.Length == 3 ? code : string.Empty;
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
