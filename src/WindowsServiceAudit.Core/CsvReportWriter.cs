using System.Globalization;
using System.Reflection;
using System.Text;

namespace WindowsServiceAudit.Core;

public static class CsvReportWriter
{
    private static readonly PropertyInfo[] ExportProperties =
    [
        Property(nameof(ServiceAuditRecord.Status)),
        Property(nameof(ServiceAuditRecord.Name)),
        Property(nameof(ServiceAuditRecord.DisplayName)),
        Property(nameof(ServiceAuditRecord.WorkingSetMB)),
        Property(nameof(ServiceAuditRecord.State)),
        Property(nameof(ServiceAuditRecord.StartMode)),
        Property(nameof(ServiceAuditRecord.DelayedAutoStart)),
        Property(nameof(ServiceAuditRecord.TriggerConfigurationPresent)),
        Property(nameof(ServiceAuditRecord.StartName)),
        Property(nameof(ServiceAuditRecord.ReviewSignals)),
        Property(nameof(ServiceAuditRecord.ExecutablePath)),
        Property(nameof(ServiceAuditRecord.PathCandidates)),
        Property(nameof(ServiceAuditRecord.PathStatus)),
        Property(nameof(ServiceAuditRecord.UnquotedPathWithSpaces)),
        Property(nameof(ServiceAuditRecord.ExecutableTarget)),
        Property(nameof(ServiceAuditRecord.CommandLine)),
        Property(nameof(ServiceAuditRecord.Description)),
        Property(nameof(ServiceAuditRecord.Category)),
        Property(nameof(ServiceAuditRecord.ClassificationEvidence)),
        Property(nameof(ServiceAuditRecord.ProcessId)),
        Property(nameof(ServiceAuditRecord.Dependencies)),
        Property(nameof(ServiceAuditRecord.Dependents)),
        Property(nameof(ServiceAuditRecord.SignatureStatus)),
        Property(nameof(ServiceAuditRecord.Signer))
    ];

    public static IReadOnlyList<string> Headers => ExportProperties.Select(property => property.Name).ToArray();

    public static void Write(string path, IEnumerable<ServiceAuditRecord> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', ExportProperties.Select(property => Escape(property.Name))));
        foreach (var record in records.OrderBy(record => record.Status == "REVIEW" ? 0 : 1)
                     .ThenBy(record => record.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            builder.AppendLine(string.Join(',', ExportProperties.Select(property => Escape(Format(property.GetValue(record))))));
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static PropertyInfo Property(string name) => typeof(ServiceAuditRecord).GetProperty(name)!;
    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        bool boolean => boolean ? "True" : "False",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}