using RatePulse.Windows.Models;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace RatePulse.Windows.Services;

public sealed class ExchangeRateService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetQuotesAsync(IEnumerable<string> pairs, CancellationToken cancellationToken = default)
    {
        var quotes = new List<ExchangeRateQuote>();
        var ratesByBaseCurrency = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in pairs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            quotes.Add(await GetQuoteAsync(pair, ratesByBaseCurrency, cancellationToken));
        }

        return quotes;
    }

    public async Task<CurrencyConversion> ConvertViaUsdAsync(
        decimal sourceAmount,
        string sourceCurrency,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        sourceCurrency = sourceCurrency.Trim().ToUpperInvariant();
        targetCurrency = targetCurrency.Trim().ToUpperInvariant();

        if (sourceCurrency.Length != 3 || targetCurrency.Length != 3)
        {
            throw new InvalidOperationException("Currency codes must use three letters, for example CNY or JPY.");
        }

        if (sourceAmount < 0)
        {
            throw new InvalidOperationException("Amount cannot be negative.");
        }

        var ratesByBaseCurrency = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var usdRoot = await GetRatesDocumentAsync("USD", ratesByBaseCurrency, cancellationToken);
        var updatedAt = GetUpdatedAt(usdRoot);
        var usdToSourceRate = GetUsdRate(usdRoot, sourceCurrency);
        var usdToTargetRate = GetUsdRate(usdRoot, targetCurrency);
        var usdAmount = sourceCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase)
            ? sourceAmount
            : sourceAmount / usdToSourceRate;
        var targetAmount = targetCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase)
            ? usdAmount
            : usdAmount * usdToTargetRate;

        return new CurrencyConversion
        {
            SourceAmount = sourceAmount,
            SourceCurrency = sourceCurrency,
            UsdAmount = usdAmount,
            TargetAmount = targetAmount,
            TargetCurrency = targetCurrency,
            UsdToSourceRate = usdToSourceRate,
            UsdToTargetRate = usdToTargetRate,
            UpdatedAt = updatedAt,
            Source = "open.er-api",
            IsCached = false
        };
    }

    public async Task<RateHistory> GetUsdHistoryAsync(
        string quoteCurrency,
        int days = 15,
        CancellationToken cancellationToken = default)
    {
        quoteCurrency = quoteCurrency.Trim().ToUpperInvariant();
        if (quoteCurrency.Length != 3 || quoteCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("History needs a non-USD quote currency, for example CNY or JPY.");
        }

        days = Math.Clamp(days, 2, 60);
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var startDate = endDate.AddDays(-(days - 1));
        var requestUri = string.Create(
            CultureInfo.InvariantCulture,
            $"https://api.frankfurter.dev/v2/rates?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}&base=USD&quotes={Uri.EscapeDataString(quoteCurrency)}");

        using var response = await HttpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("value", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Historical rate response did not include a value array.");
        }

        var points = new List<RateHistoryPoint>();
        foreach (var item in values.EnumerateArray())
        {
            if (!item.TryGetProperty("quote", out var quoteElement) ||
                !quoteCurrency.Equals(quoteElement.GetString(), StringComparison.OrdinalIgnoreCase) ||
                !item.TryGetProperty("date", out var dateElement) ||
                !item.TryGetProperty("rate", out var rateElement))
            {
                continue;
            }

            var dateText = dateElement.GetString();
            if (string.IsNullOrWhiteSpace(dateText) ||
                !DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, out var date))
            {
                continue;
            }

            points.Add(new RateHistoryPoint
            {
                Date = date,
                Rate = rateElement.GetDecimal()
            });
        }

        points = points
            .OrderBy(point => point.Date)
            .TakeLast(days)
            .ToList();

        if (points.Count == 0)
        {
            throw new InvalidOperationException($"Missing USD history for {quoteCurrency}.");
        }

        return new RateHistory
        {
            Pair = $"USD/{quoteCurrency}",
            Points = points,
            UpdatedAt = DateTimeOffset.Now,
            Source = "frankfurter",
            IsCached = false
        };
    }

    private static async Task<ExchangeRateQuote> GetQuoteAsync(
        string pair,
        Dictionary<string, JsonElement> ratesByBaseCurrency,
        CancellationToken cancellationToken)
    {
        var parts = pair.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid currency pair: {pair}");
        }

        var baseCurrency = parts[0].ToUpperInvariant();
        var quoteCurrency = parts[1].ToUpperInvariant();
        var root = await GetRatesDocumentAsync(baseCurrency, ratesByBaseCurrency, cancellationToken);

        if (!root.TryGetProperty("rates", out var rates) || !rates.TryGetProperty(quoteCurrency, out var rateElement))
        {
            throw new InvalidOperationException($"Missing rate for {pair}");
        }

        var rate = rateElement.GetDecimal();
        var updatedAt = GetUpdatedAt(root);

        return new ExchangeRateQuote
        {
            Pair = string.Create(CultureInfo.InvariantCulture, $"{baseCurrency}/{quoteCurrency}"),
            Rate = rate,
            UpdatedAt = updatedAt,
            Source = "open.er-api",
            IsCached = false
        };
    }

    private static decimal GetUsdRate(JsonElement usdRoot, string currency)
    {
        if (currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        if (!usdRoot.TryGetProperty("rates", out var rates) || !rates.TryGetProperty(currency, out var rateElement))
        {
            throw new InvalidOperationException($"Missing USD rate for {currency}");
        }

        return rateElement.GetDecimal();
    }

    private static DateTimeOffset GetUpdatedAt(JsonElement root)
    {
        if (root.TryGetProperty("time_last_update_unix", out var unixElement) && unixElement.TryGetInt64(out var unixTime))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        return DateTimeOffset.UtcNow;
    }

    private static async Task<JsonElement> GetRatesDocumentAsync(
        string baseCurrency,
        Dictionary<string, JsonElement> ratesByBaseCurrency,
        CancellationToken cancellationToken)
    {
        if (ratesByBaseCurrency.TryGetValue(baseCurrency, out var cachedRoot))
        {
            return cachedRoot;
        }

        var requestUri = $"https://open.er-api.com/v6/latest/{Uri.EscapeDataString(baseCurrency)}";

        using var response = await HttpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement.Clone();
        ratesByBaseCurrency[baseCurrency] = root;
        return root;
    }
}
