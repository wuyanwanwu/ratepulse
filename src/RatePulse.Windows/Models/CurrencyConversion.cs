using System.Text.Json.Serialization;

namespace RatePulse.Windows.Models;

public sealed class CurrencyConversion
{
    public required decimal SourceAmount { get; init; }

    public required string SourceCurrency { get; init; }

    public required decimal UsdAmount { get; init; }

    public required decimal TargetAmount { get; init; }

    public required string TargetCurrency { get; init; }

    public required decimal UsdToSourceRate { get; init; }

    public required decimal UsdToTargetRate { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Source { get; init; }

    public bool IsCached { get; init; }

    [JsonIgnore]
    public string SourceAmountText => FormatAmount(SourceAmount);

    [JsonIgnore]
    public string UsdAmountText => FormatAmount(UsdAmount);

    [JsonIgnore]
    public string TargetAmountText => FormatAmount(TargetAmount);

    [JsonIgnore]
    public string SourceDisplayText => $"{SourceAmountText} {SourceCurrency}";

    [JsonIgnore]
    public string UsdDisplayText => $"{UsdAmountText} USD";

    [JsonIgnore]
    public string TargetDisplayText => $"{TargetAmountText} {TargetCurrency}";

    [JsonIgnore]
    public string RateSummaryText => $"USD/{SourceCurrency}: {UsdToSourceRate:0.####}  USD/{TargetCurrency}: {UsdToTargetRate:0.####}";

    [JsonIgnore]
    public string UpdatedAtText => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    [JsonIgnore]
    public string DataStateText => IsCached ? "cached" : "fresh";

    public CurrencyConversion WithCacheState(bool isCached)
    {
        return new CurrencyConversion
        {
            SourceAmount = SourceAmount,
            SourceCurrency = SourceCurrency,
            UsdAmount = UsdAmount,
            TargetAmount = TargetAmount,
            TargetCurrency = TargetCurrency,
            UsdToSourceRate = UsdToSourceRate,
            UsdToTargetRate = UsdToTargetRate,
            UpdatedAt = UpdatedAt,
            Source = Source,
            IsCached = isCached
        };
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.####");
    }
}
