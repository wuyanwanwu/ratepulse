using System.Text.Json.Serialization;

namespace RatePulse.Windows.Models;

public sealed class RateHistoryPoint
{
    public required DateOnly Date { get; init; }

    public required decimal Rate { get; init; }

    public string? Source { get; init; }
}

public sealed class RateHistory
{
    public required string Pair { get; init; }

    public required List<RateHistoryPoint> Points { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Source { get; init; }

    public bool IsCached { get; init; }

    [JsonIgnore]
    public string UpdatedAtText => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    [JsonIgnore]
    public string DataStateText => IsCached ? "cached" : "fresh";

    public RateHistory WithCacheState(bool isCached)
    {
        return new RateHistory
        {
            Pair = Pair,
            Points = [.. Points],
            UpdatedAt = UpdatedAt,
            Source = Source,
            IsCached = isCached
        };
    }
}
