using System.IO;
using System.Text.Json;

namespace WindowsServiceAudit.App;

internal static class AccessibilitySettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsServiceAudit", "accessibility.json");

    public static bool Load() => Load(SettingsPath);

    internal static bool Load(string path)
    {
        if (!File.Exists(path)) return false;
        return JsonSerializer.Deserialize<bool>(File.ReadAllText(path));
    }

    public static void Save(bool enabled) => Save(SettingsPath, enabled);

    internal static void Save(string path, bool enabled)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(enabled));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
