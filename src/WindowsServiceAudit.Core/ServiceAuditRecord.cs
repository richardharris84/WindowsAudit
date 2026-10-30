namespace WindowsServiceAudit.Core;

public sealed class ServiceAuditRecord
{
    public string Status { get; set; } = "REVIEW";
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double? WorkingSetMB { get; set; }
    public string State { get; set; } = "Unknown";
    public string StartMode { get; set; } = "Unknown";
    public bool? DelayedAutoStart { get; set; }
    public bool? TriggerConfigurationPresent { get; set; }
    public string StartName { get; set; } = string.Empty;
    public string ReviewSignals { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string PathCandidates { get; set; } = string.Empty;
    public string PathStatus { get; set; } = "No image path reported";
    public bool UnquotedPathWithSpaces { get; set; }
    public string ExecutableTarget { get; set; } = "Unavailable";
    public string CommandLine { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Unknown; manual review";
    public string ClassificationEvidence { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string Dependencies { get; set; } = string.Empty;
    public string Dependents { get; set; } = string.Empty;
    public string SignatureStatus { get; set; } = "Not checked";
    public string Signer { get; set; } = string.Empty;
}

public sealed record AuditProgress(string Message, int Completed, int Total);