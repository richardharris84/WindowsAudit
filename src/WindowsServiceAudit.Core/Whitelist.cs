namespace WindowsServiceAudit.Core;

public static class Whitelist
{
    public const string Header = "WhitelistTerm";

    public static IReadOnlyList<string> DefaultTerms { get; } =
    [
        "Adobe", "system32", "Battle.net", "Google", "Logi", "Windows Defender",
        "Edge", "Steam", "Microsoft.NET", "WSL", "Dropbox", "Copilot", "WINDOWS",
        "Microsoft Shared", "McmSvc", "NetSetupSvc"
    ];

    public static IReadOnlyList<string> LoadOrSeed(string outputDirectory)
    {
        var path = Path.Combine(outputDirectory, "whitelist.csv");
        if (!File.Exists(path)) Save(path, DefaultTerms);

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2 || !string.Equals(Unquote(lines[0]), Header, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Whitelist CSV must contain a '{Header}' header and at least one term: {path}");

        var terms = lines.Skip(1).Select(Unquote).Select(term => term.Trim())
            .Where(term => term.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (terms.Length == 0) throw new InvalidDataException($"No whitelist terms were found in: {path}");
        return terms;
    }

    public static void Save(string path, IEnumerable<string> terms)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllLines(path, new[] { Header }.Concat(terms.Select(Escape)));
    }

    public static bool Matches(string? executablePath, string? executableTarget, IReadOnlyList<string> terms) =>
        terms.Any(term => (!string.IsNullOrWhiteSpace(executablePath) && executablePath.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                          (!string.IsNullOrWhiteSpace(executableTarget) && executableTarget.Contains(term, StringComparison.OrdinalIgnoreCase)));

    private static string Escape(string value) => $"\"{value.Trim().Replace("\"", "\"\"")}\"";
    private static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1].Replace("\"\"", "\"")
            : value;
    }
}