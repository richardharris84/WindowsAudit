using System.Security;

namespace WindowsServiceAudit.Core;

public static class AuditDataDirectory
{
    public static string Resolve(string applicationDirectory, string fallbackDirectory, string? configuredDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
            return SelectConfiguredDirectory(configuredDirectory, CanWriteToDirectory);

        return SelectWritableDirectory(applicationDirectory, fallbackDirectory, CanWriteToDirectory);
    }

    public static string SelectConfiguredDirectory(string configuredDirectory, Func<string, bool> isWritable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredDirectory);
        ArgumentNullException.ThrowIfNull(isWritable);

        var path = Path.GetFullPath(configuredDirectory);
        if (isWritable(path)) return path;

        throw new UnauthorizedAccessException($"The configured audit folder is not writable: {path}");
    }

    public static string SelectWritableDirectory(
        string applicationDirectory,
        string fallbackDirectory,
        Func<string, bool> isWritable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackDirectory);
        ArgumentNullException.ThrowIfNull(isWritable);

        var applicationPath = Path.GetFullPath(applicationDirectory);
        var fallbackPath = Path.GetFullPath(fallbackDirectory);
        if (isWritable(applicationPath)) return applicationPath;
        if (isWritable(fallbackPath)) return fallbackPath;

        throw new UnauthorizedAccessException(
            $"Neither the application folder nor the fallback folder is writable. Tried: {applicationPath} and {fallbackPath}");
    }

    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".windows-service-audit-{Guid.NewGuid():N}.tmp");
            using var probe = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            probe.WriteByte(0);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }
}