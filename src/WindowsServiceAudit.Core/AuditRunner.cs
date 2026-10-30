using System.Management;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace WindowsServiceAudit.Core;

public sealed class AuditRunner
{
    private static readonly HashSet<string> CoreServiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DcomLaunch", "EventLog", "LSM", "PlugPlay", "Power", "RpcEptMapper", "RpcSs", "SamSs", "SystemEventsBroker"
    };

    public async Task<IReadOnlyList<ServiceAuditRecord>> RunAsync(
        string outputDirectory,
        int sampleSeconds = 1,
        IProgress<AuditProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        if (sampleSeconds is < 1 or > 300) throw new ArgumentOutOfRangeException(nameof(sampleSeconds));

        Directory.CreateDirectory(outputDirectory);
        var terms = Whitelist.LoadOrSeed(outputDirectory);
        progress?.Report(new AuditProgress("Reading the local Windows service inventory…", 0, 0));
        var records = ReadServices(terms, cancellationToken);

        var processCount = records.Where(record => record.ProcessId > 0).Select(record => record.ProcessId).Distinct().Count();
        progress?.Report(new AuditProgress($"Waiting {sampleSeconds} second(s) before taking process working-set readings…", 0, processCount));
        await Task.Delay(TimeSpan.FromSeconds(sampleSeconds), cancellationToken);
        CaptureWorkingSets(records, progress, cancellationToken);

        progress?.Report(new AuditProgress("Writing CSV and RESULTS report…", records.Count, records.Count));
        CsvReportWriter.Write(Path.Combine(outputDirectory, "Win-Srv-Eval-Results.csv"), records);
        ResultsWriter.Update(Path.Combine(outputDirectory, "Win-Srv-Eval-Report.md"), records);
        progress?.Report(new AuditProgress("Audit complete.", records.Count, records.Count));
        return records;
    }

    private static List<ServiceAuditRecord> ReadServices(IReadOnlyList<string> whitelist, CancellationToken cancellationToken)
    {
        var records = new List<ServiceAuditRecord>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, DisplayName, State, StartMode, StartName, ProcessId, PathName, Description FROM Win32_Service");
        using var services = searcher.Get();

        foreach (ManagementObject service in services)
        {
            using (service)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = GetString(service, "Name");
                if (name.Length == 0) continue;

                var commandLine = GetString(service, "PathName");
                var parsed = ServicePathParser.Parse(commandLine);
                var registry = ReadRegistryMetadata(name);
                var target = ServicePathParser.GetTarget(parsed.ExecutablePath, registry.ImagePath, registry.ServiceDll);
                var executablePath = ServicePathParser.Expand(parsed.ExecutablePath);
                var pathStatus = GetPathStatus(parsed, executablePath);
                var (signatureStatus, signer) = GetSigner(executablePath);
                var (category, evidence) = Classify(name, signatureStatus, signer);
                var state = GetString(service, "State", "Unknown");
                var startMode = GetString(service, "StartMode", "Unknown");
                var reviewSignals = new List<string>();

                if (startMode.Equals("Auto", StringComparison.OrdinalIgnoreCase) && !state.Equals("Running", StringComparison.OrdinalIgnoreCase))
                    reviewSignals.Add("Automatic-start service is currently stopped; this may be normal or trigger-based.");
                if (parsed.UnquotedPathWithSpaces)
                    reviewSignals.Add("Unquoted executable candidate contains spaces; review the service image path.");
                if (pathStatus == "File not found")
                    reviewSignals.Add("Resolved executable path was not found.");

                records.Add(new ServiceAuditRecord
                {
                    Status = Whitelist.Matches(parsed.ExecutablePath, target, whitelist) ? "OK" : "REVIEW",
                    Name = name,
                    DisplayName = GetString(service, "DisplayName"),
                    State = state,
                    StartMode = startMode,
                    DelayedAutoStart = registry.DelayedAutoStart,
                    TriggerConfigurationPresent = registry.TriggerPresent,
                    StartName = GetString(service, "StartName"),
                    ReviewSignals = string.Join("; ", reviewSignals),
                    ExecutablePath = parsed.ExecutablePath,
                    PathCandidates = parsed.PathCandidates,
                    PathStatus = pathStatus,
                    UnquotedPathWithSpaces = parsed.UnquotedPathWithSpaces,
                    ExecutableTarget = target,
                    CommandLine = commandLine,
                    Description = GetString(service, "Description"),
                    Category = category,
                    ClassificationEvidence = evidence,
                    ProcessId = GetInt(service, "ProcessId"),
                    Dependencies = registry.Dependencies,
                    Dependents = registry.Dependents,
                    SignatureStatus = signatureStatus,
                    Signer = signer
                });
            }
        }

        return records.OrderBy(record => record.Status == "REVIEW" ? 0 : 1)
            .ThenBy(record => record.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static void CaptureWorkingSets(List<ServiceAuditRecord> records, IProgress<AuditProgress>? progress, CancellationToken cancellationToken)
    {
        var groups = records.Where(record => record.ProcessId > 0).GroupBy(record => record.ProcessId).ToArray();
        var completed = 0;
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double? workingSet = null;
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(group.Key);
                workingSet = Math.Round(process.WorkingSet64 / (1024d * 1024d), 1);
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }

            foreach (var record in group) record.WorkingSetMB = workingSet;
            completed++;
            progress?.Report(new AuditProgress("Capturing process working sets…", completed, groups.Length));
        }
    }

    private static (string ImagePath, string ServiceDll, bool? DelayedAutoStart, bool? TriggerPresent, string Dependencies, string Dependents) ReadRegistryMetadata(string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}");
            if (key is null) return ("", "", null, null, "", "");
            using var parameters = key.OpenSubKey("Parameters");
            using var trigger = key.OpenSubKey("TriggerInfo");
            var delayedValue = key.GetValue("DelayedAutoStart");
            bool? delayed = delayedValue is null ? null : Convert.ToInt32(delayedValue) != 0;
            var dependencies = key.GetValue("DependOnService") as string[] ?? [];
            return (Convert.ToString(key.GetValue("ImagePath")) ?? "",
                Convert.ToString(parameters?.GetValue("ServiceDll")) ?? "",
                delayed,
                trigger is not null,
                string.Join("; ", dependencies),
                "");
        }
        catch (UnauthorizedAccessException)
        {
            return ("", "", null, null, "", "");
        }
        catch (System.Security.SecurityException)
        {
            return ("", "", null, null, "", "");
        }
    }

    private static (string Status, string Signer) GetSigner(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return ("Not checked", "");
        try
        {
            using var certificate = X509CertificateLoader.LoadCertificateFromFile(path);
            return ("Signer certificate present; trust not evaluated", certificate.Subject);
        }
        catch (System.Security.Cryptography.CryptographicException) { return ("No embedded signer certificate", ""); }
        catch (UnauthorizedAccessException) { return ("Check unavailable", ""); }
    }

    private static string GetPathStatus(ParsedServicePath parsed, string path)
    {
        if (parsed.PathStatus is "No image path reported" or "Malformed quoted image path" or "Image path could not be parsed") return parsed.PathStatus;
        if (path.Contains('%')) return "Unresolved environment variable";
        if (!Path.IsPathRooted(path)) return "Unresolved relative path";
        return File.Exists(path) ? "File exists" : "File not found";
    }

    private static (string Category, string Evidence) Classify(string name, string signatureStatus, string signer)
    {
        if (CoreServiceNames.Contains(name))
            return ("Curated Windows core service", "Service name matches the app curated core list; verify Windows version and role before any change.");
        if (signer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
            return ("Microsoft signer present; role-dependent", "An embedded Microsoft signer certificate was found; signature trust was not evaluated and this does not establish that the service is optional.");
        if (signer.Length > 0)
            return ("Third-party signer present; manual review", "An embedded signer certificate was found; signature trust was not evaluated. Assess the installed product and your usage.");
        return ("Unknown; manual review", $"No publisher identity was established ({signatureStatus}); this is not evidence that the service is unnecessary.");
    }

    private static string GetString(ManagementBaseObject item, string property, string fallback = "") => Convert.ToString(item[property]) ?? fallback;
    private static int GetInt(ManagementBaseObject item, string property) => item[property] is null ? 0 : Convert.ToInt32(item[property]);
}