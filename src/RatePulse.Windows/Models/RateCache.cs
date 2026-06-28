namespace RatePulse.Windows.Models;

public sealed class RateCache
{
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.MinValue;

    public List<ExchangeRateQuote> Quotes { get; set; } = [];

    public CurrencyConversion? Conversion { get; set; }
}
