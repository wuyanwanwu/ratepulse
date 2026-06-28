namespace RatePulse.Windows.Models;

public sealed class ExchangeRateQuote
{
    public required string Pair { get; init; }

    public required decimal Rate { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Source { get; init; }

    public string RateText => Rate.ToString("0.####");

    public string UpdatedAtText => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
