using System.Text.RegularExpressions;

namespace WindowsServiceAudit.Core;

public sealed record ParsedServicePath(string ExecutablePath, string PathCandidates, string PathStatus, bool UnquotedPathWithSpaces);

public static partial class ServicePathParser
{
    [GeneratedRegex("\\.exe(?=\\s|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExecutableBoundaryRegex();

    public static ParsedServicePath Parse(string? commandLine)
    {
        var trimmed = commandLine?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return new ParsedServicePath("", "", "No image path reported", false);

        if (trimmed.StartsWith('"'))
        {
            var closingQuote = trimmed.IndexOf('"', 1);
            if (closingQuote < 0) return new ParsedServicePath("", "", "Malformed quoted image path", false);
            var quotedPath = trimmed[1..closingQuote];
            return new ParsedServicePath(quotedPath, quotedPath, "Quoted image path", false);
        }

        var candidates = ExecutableBoundaryRegex().Matches(trimmed)
            .Select(match => trimmed[..(match.Index + match.Length)].Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hasSpaces = candidates.Any(candidate => candidate.Any(char.IsWhiteSpace));
        var existing = candidates.Where(File.Exists).ToArray();
        if (existing.Length > 1) return new ParsedServicePath("", string.Join("; ", candidates), "Ambiguous unquoted image path", hasSpaces);
        if (existing.Length == 1) return new ParsedServicePath(existing[0], string.Join("; ", candidates), "Resolved unquoted image path", hasSpaces);
        if (candidates.Length > 0) return new ParsedServicePath(candidates[^1], string.Join("; ", candidates), "Candidate image path not found", hasSpaces);
        return new ParsedServicePath("", "", "Image path could not be parsed", false);
    }

    public static string Expand(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        if (expanded.StartsWith("\\SystemRoot", StringComparison.OrdinalIgnoreCase))
            expanded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), expanded["\\SystemRoot".Length..].TrimStart('\\'));
        return expanded;
    }

    public static string GetTarget(string executablePath, string? registryImagePath, string? serviceDll)
    {
        if (!string.IsNullOrWhiteSpace(executablePath)) return executablePath;
        var dll = Expand(serviceDll);
        var parsedHost = Parse(registryImagePath);
        var host = Expand(parsedHost.ExecutablePath);
        if (dll.Length > 0 && host.Length > 0) return $"{dll} (loaded by {host})";
        if (dll.Length > 0) return dll;
        if (host.Length > 0) return $"{host} (service payload not identified)";
        if (!string.IsNullOrWhiteSpace(registryImagePath)) return $"Registry image command: {registryImagePath} (target could not be parsed)";
        return "Unavailable";
    }
}