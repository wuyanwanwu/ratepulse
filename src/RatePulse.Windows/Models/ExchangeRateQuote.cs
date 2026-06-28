using System.Text.Json.Serialization;

namespace RatePulse.Windows.Models;

public sealed class ExchangeRateQuote
{
    public required string Pair { get; init; }

    public required decimal Rate { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Source { get; init; }

    public bool IsCached { get; init; }

    [JsonIgnore]
    public string? DisplayPairOverride { get; init; }

    [JsonIgnore]
    public string? DisplayDataStateOverride { get; init; }

    [JsonIgnore]
    public string DisplayPair => DisplayPairOverride ?? Pair;

    [JsonIgnore]
    public string DisplayDataState => DisplayDataStateOverride ?? DataStateText;

    [JsonIgnore]
    public string RateText => Rate.ToString("0.####");

    [JsonIgnore]
    public string UpdatedAtText => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    [JsonIgnore]
    public string DataStateText => IsCached ? "cached" : "fresh";

    public ExchangeRateQuote WithCacheState(bool isCached)
    {
        return new ExchangeRateQuote
        {
            Pair = Pair,
            Rate = Rate,
            UpdatedAt = UpdatedAt,
            Source = Source,
            IsCached = isCached,
            DisplayPairOverride = DisplayPairOverride,
            DisplayDataStateOverride = DisplayDataStateOverride
        };
    }

    public ExchangeRateQuote WithDisplayText(string displayPair, string displayDataState)
    {
        return new ExchangeRateQuote
        {
            Pair = Pair,
            Rate = Rate,
            UpdatedAt = UpdatedAt,
            Source = Source,
            IsCached = IsCached,
            DisplayPairOverride = displayPair,
            DisplayDataStateOverride = displayDataState
        };
    }
}
