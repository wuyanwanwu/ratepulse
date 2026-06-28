using System.IO;

namespace RatePulse.Windows.Services;

public static class AppStoragePaths
{
    public static string AppDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RatePulse");

    public static string SettingsPath => Path.Combine(AppDirectory, "settings.json");

    public static string CachePath => Path.Combine(AppDirectory, "rate-cache.json");

    public static void EnsureAppDirectory()
    {
        Directory.CreateDirectory(AppDirectory);
    }
}
