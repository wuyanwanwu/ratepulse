using RatePulse.Windows.Models;
using System.IO;
using System.Text.Json;

namespace RatePulse.Windows.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();

        if (!File.Exists(AppStoragePaths.SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(AppStoragePaths.SettingsPath);
            AppSettings? settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
            settings ??= new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        AppStoragePaths.EnsureAppDirectory();
        settings.Normalize();

        await using var stream = File.Create(AppStoragePaths.SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
