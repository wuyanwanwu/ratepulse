using RatePulse.Windows.Models;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace RatePulse.Windows.Services;

public sealed class ExchangeRateService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetQuotesAsync(IEnumerable<string> pairs, CancellationToken cancellationToken = default)
    {
        var quotes = new List<ExchangeRateQuote>();

        foreach (var pair in pairs)
        {
            quotes.Add(await GetQuoteAsync(pair, cancellationToken));
        }

        return quotes;
    }

    private static async Task<ExchangeRateQuote> GetQuoteAsync(string pair, CancellationToken cancellationToken)
    {
        var parts = pair.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid currency pair: {pair}");
        }

        var baseCurrency = parts[0].ToUpperInvariant();
        var quoteCurrency = parts[1].ToUpperInvariant();
        var requestUri = $"https://open.er-api.com/v6/latest/{Uri.EscapeDataString(baseCurrency)}";

        using var response = await HttpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!root.TryGetProperty("rates", out var rates) || !rates.TryGetProperty(quoteCurrency, out var rateElement))
        {
            throw new InvalidOperationException($"Missing rate for {pair}");
        }

        var rate = rateElement.GetDecimal();
        var updatedAt = DateTimeOffset.UtcNow;

        if (root.TryGetProperty("time_last_update_unix", out var unixElement) && unixElement.TryGetInt64(out var unixTime))
        {
            updatedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        return new ExchangeRateQuote
        {
            Pair = string.Create(CultureInfo.InvariantCulture, $"{baseCurrency}/{quoteCurrency}"),
            Rate = rate,
            UpdatedAt = updatedAt,
            Source = "open.er-api"
        };
    }
}
