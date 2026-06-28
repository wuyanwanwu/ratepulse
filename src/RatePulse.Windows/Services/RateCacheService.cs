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

    public async Task<IReadOnlyList<ExchangeRateQuote>> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();

        if (!File.Exists(AppStoragePaths.CachePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(AppStoragePaths.CachePath);
            var cache = await JsonSerializer.DeserializeAsync<RateCache>(stream, JsonOptions, cancellationToken);
            return cache?.Quotes.Select(quote => quote.WithCacheState(true)).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveAsync(IEnumerable<ExchangeRateQuote> quotes, CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();

        var cache = new RateCache
        {
            SavedAt = DateTimeOffset.Now,
            Quotes = quotes.Select(quote => quote.WithCacheState(false)).ToList()
        };

        await using var stream = File.Create(AppStoragePaths.CachePath);
        await JsonSerializer.SerializeAsync(stream, cache, JsonOptions, cancellationToken);
    }
}
