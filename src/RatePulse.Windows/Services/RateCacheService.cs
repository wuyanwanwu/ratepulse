using RatePulse.Windows.Models;
using System.IO;
using System.Text.Json;

namespace RatePulse.Windows.Services;

public sealed class RateCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<RateCache> LoadCacheAsync(CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();

        if (!File.Exists(AppStoragePaths.CachePath))
        {
            return new RateCache();
        }

        try
        {
            await using var stream = File.OpenRead(AppStoragePaths.CachePath);
            var cache = await JsonSerializer.DeserializeAsync<RateCache>(stream, JsonOptions, cancellationToken);
            cache ??= new RateCache();
            cache.Quotes = cache.Quotes.Select(quote => quote.WithCacheState(true)).ToList();
            cache.Conversion = cache.Conversion?.WithCacheState(true);
            return cache;
        }
        catch
        {
            return new RateCache();
        }
    }

    public async Task<IReadOnlyList<ExchangeRateQuote>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var cache = await LoadCacheAsync(cancellationToken);
        return cache.Quotes;
    }

    public async Task SaveAsync(
        IEnumerable<ExchangeRateQuote> quotes,
        CurrencyConversion? conversion,
        CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();

        var cache = new RateCache
        {
            SavedAt = DateTimeOffset.Now,
            Quotes = quotes.Select(quote => quote.WithCacheState(false)).ToList(),
            Conversion = conversion?.WithCacheState(false)
        };

        await using var stream = File.Create(AppStoragePaths.CachePath);
        await JsonSerializer.SerializeAsync(stream, cache, JsonOptions, cancellationToken);
    }
}
