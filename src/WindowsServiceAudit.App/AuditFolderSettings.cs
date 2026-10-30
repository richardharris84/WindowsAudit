using System.IO;
using System.Text.Json;

namespace WindowsServiceAudit.App;

internal static class AuditFolderSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsServiceAudit");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    public static string? Load()
    {
        if (!File.Exists(SettingsPath)) return null;

        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
        if (string.IsNullOrWhiteSpace(settings?.AuditDirectory))
            throw new InvalidDataException($"The saved audit folder setting is invalid: {SettingsPath}");

        return settings.AuditDirectory;
    }

    public static void Save(string auditDirectory)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var temporaryPath = Path.Combine(SettingsDirectory, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new Settings { AuditDirectory = auditDirectory }));
            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private sealed class Settings
    {
        public string? AuditDirectory { get; set; }
    }
}