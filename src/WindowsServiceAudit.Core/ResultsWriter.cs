using System.Text;
using System.Globalization;

namespace WindowsServiceAudit.Core;

public static class ResultsWriter
{
    public const string BeginMarker = "<!-- BEGIN AUTO-GENERATED NON-WHITELIST REVIEWS -->";
    public const string EndMarker = "<!-- END AUTO-GENERATED NON-WHITELIST REVIEWS -->";

    public static void Update(string path, IReadOnlyList<ServiceAuditRecord> records)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            WriteReport(path, GenerateReport(records));
            return;
        }

        var content = File.ReadAllText(path);
        if (IsKnownReport(content))
        {
            WriteReport(path, GenerateReport(records));
            return;
        }

        var begin = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        var end = begin < 0 ? -1 : content.IndexOf(EndMarker, begin, StringComparison.Ordinal);
        if (begin < 0 || end < 0)
        {
            throw new InvalidDataException($"Existing report file is not a recognized generated report and does not contain the generated-section markers; it was left unchanged: {path}");
        }

        var generated = GenerateReviewSection(records);
        content = content[..(begin + BeginMarker.Length)] + Environment.NewLine + generated + Environment.NewLine + content[end..];
        WriteReport(path, content);
    }

    public static string GenerateReport(IEnumerable<ServiceAuditRecord> services, DateTime? generatedAt = null)
    {
        var records = services.ToArray();
        var reviewCount = records.Count(service => service.Status == "REVIEW");
        var runningCount = records.Count(service => service.State.Equals("Running", StringComparison.OrdinalIgnoreCase));
        var automaticCount = records.Count(service => service.StartMode.Equals("Auto", StringComparison.OrdinalIgnoreCase));
        var date = (generatedAt ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            "# Windows Service Audit Results",
            "",
            $"Generated: {date}",
            "",
            "## Summary",
            "",
            $"- **Services inventoried:** {records.Length:N0}",
            $"- **Marked OK by whitelist:** {records.Length - reviewCount:N0}",
            $"- **Marked REVIEW:** {reviewCount:N0}",
            $"- **Currently running:** {runningCount:N0}",
            $"- **Automatic startup:** {automaticCount:N0}",
            "",
            "## How to Read This Report",
            "",
            "`OK` and `REVIEW` are text matches against the whitelist, not safety or importance ratings. `REVIEW` means the service's reported executable path and resolved target did not match a whitelist term. It does not mean the service is harmful or should be removed. `OK` does not certify that a service or file is safe.",
            "",
            "Use the service description, publisher information, executable location, startup setting, and the software installed on this computer to investigate a result. Do not stop or disable a service based on this report alone.",
            "",
            "## Services to Review",
            GenerateReviewSection(records),
            "",
            "## Important Limitations",
            "",
            "- This report is generated from the current local Windows inventory and whitelist. It does not use a fixed list of example services or fetch Google results automatically.",
            "- A signing certificate's presence does not mean Windows trust or file reputation was evaluated.",
            "- Working-set memory is a process snapshot taken once after the selected interval. Services sharing a host process, such as `svchost.exe`, cannot be measured separately.",
            "- Missing metadata can result from Windows permissions, service configuration, or a process that has exited.",
            "- No CPU usage measurement is included.",
            ""
        };
        return string.Join(Environment.NewLine, lines);
    }

    public static string GenerateReviewSection(IEnumerable<ServiceAuditRecord> services)
    {
        var review = services.Where(service => service.Status == "REVIEW")
            .OrderBy(service => service.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (review.Length == 0) return "No services currently have `REVIEW` status.";

        var lines = new List<string>
        {
            "The following services did not match the current whitelist. This is a prompt to identify the owning software, not a safety verdict."
        };
        foreach (var service in review)
        {
            lines.Add(string.Empty);
            lines.Add($"### {MarkdownText(service.DisplayName)} (`{Code(service.Name)}`)");
            lines.Add(string.Empty);
            lines.Add($"- **Why it is listed:** Its executable path and resolved target did not match a term in the current whitelist.");
            lines.Add($"- **Current state:** {MarkdownText(Display(service.State))}.");
            lines.Add($"- **Startup setting:** {MarkdownText(Display(service.StartMode))}; account: {Code(Display(service.StartName))}.");
            lines.Add($"- **Executable path:** {Code(Display(service.ExecutablePath))}.");
            lines.Add($"- **Resolved target:** {MarkdownText(Display(service.ExecutableTarget))}.");
            lines.Add($"- **File path check:** {MarkdownText(Display(service.PathStatus))}.");
            lines.Add($"- **Publisher information:** {MarkdownText(Display(service.Signer))}; {MarkdownText(Display(service.SignatureStatus))}. A listed signer is not a trust verdict.");
            lines.Add($"- **Classification context:** {MarkdownText(Display(service.Category))}. {MarkdownText(Display(service.ClassificationEvidence))}");
            if (service.ProcessId > 0) lines.Add($"- **Host process ID:** {service.ProcessId}.");
            if (service.WorkingSetMB is not null)
                lines.Add($"- **Host process memory snapshot:** {service.WorkingSetMB.Value.ToString("N1", CultureInfo.InvariantCulture)} MB. This may be shared with other services.");
            if (!string.IsNullOrWhiteSpace(service.ReviewSignals)) lines.Add($"- **Additional observations:** {MarkdownText(service.ReviewSignals)}");
            if (!string.IsNullOrWhiteSpace(service.Dependencies)) lines.Add($"- **Depends on:** {MarkdownText(service.Dependencies)}.");
            if (!string.IsNullOrWhiteSpace(service.Dependents)) lines.Add($"- **Services that depend on this service:** {MarkdownText(service.Dependents)}.");
            lines.Add(!string.IsNullOrWhiteSpace(service.Description)
                ? $"- **What this service does:** {MarkdownText(service.Description)}"
                : "- **What this service does:** Windows did not provide a service description.");
            var searchQuery = $"{service.DisplayName} ({service.Name})";
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(searchQuery)}";
            lines.Add($"- **Google Search:** [{MarkdownText(service.DisplayName)} (`{Code(service.Name)}`)]({searchUrl}).");
            lines.Add("- **Safety note:** Search results and this report do not establish whether the service is safe to disable.");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsKnownReport(string content)
    {
        var firstLine = content.TrimStart('\uFEFF', ' ', '\t', '\r', '\n')
            .Split(['\r', '\n'], 2)[0].Trim();
        return firstLine is "# Windows Service Audit Results" or "# Service Evaluation Summary";
    }

    private static void WriteReport(string path, string content) =>
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "not reported" : value;

    private static string Code(string value) => value.Replace('`', '\'').Replace('\r', ' ').Replace('\n', ' ');

    private static string MarkdownText(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("`", "\\`", StringComparison.Ordinal)
        .Replace("*", "\\*", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}