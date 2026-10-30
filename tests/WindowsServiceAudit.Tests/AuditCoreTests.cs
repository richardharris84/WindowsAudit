using WindowsServiceAudit.Core;
using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsServiceAudit.Tests;

[TestClass]
public sealed class AuditCoreTests
{
    [TestMethod]
    public void WhitelistSeedsDefaultsOnlyWhenFileIsMissingAndPreservesEdits()
    {
        var directory = CreateTempDirectory();
        try
        {
            var seeded = Whitelist.LoadOrSeed(directory);
            var whitelistPath = Path.Combine(directory, "whitelist.csv");
            Assert.IsTrue(File.Exists(whitelistPath));
            Assert.IsTrue(seeded.Contains("McmSvc"));

            Whitelist.Save(whitelistPath, ["My Custom Vendor"]);
            var loaded = Whitelist.LoadOrSeed(directory);
            CollectionAssert.AreEqual(new[] { "My Custom Vendor" }, loaded.ToArray());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void WhitelistMatchesDirectOrFallbackTargetCaseInsensitively()
    {
        var terms = new[] { "system32", "Vendor Folder" };

        Assert.IsTrue(Whitelist.Matches(null, @"C:\Windows\System32\service.dll (loaded by svchost.exe)", terms));
        Assert.IsTrue(Whitelist.Matches(@"C:\Program Files\VENDOR FOLDER\service.exe", null, terms));
        Assert.IsFalse(Whitelist.Matches(@"C:\Program Files\Other\service.exe", "Unavailable", terms));
    }

    [TestMethod]
    public void AuditDataDirectoryPrefersApplicationDirectoryWhenWritable()
    {
        var applicationDirectory = Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "app");
        var fallbackDirectory = Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "user");

        var selected = AuditDataDirectory.SelectWritableDirectory(
            applicationDirectory,
            fallbackDirectory,
            _ => true);

        Assert.AreEqual(Path.GetFullPath(applicationDirectory), selected);
    }

    [TestMethod]
    public void AuditDataDirectoryUsesFallbackWhenApplicationDirectoryIsNotWritable()
    {
        var applicationDirectory = Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "app");
        var fallbackDirectory = Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "user");

        var selected = AuditDataDirectory.SelectWritableDirectory(
            applicationDirectory,
            fallbackDirectory,
            path => path == Path.GetFullPath(fallbackDirectory));

        Assert.AreEqual(Path.GetFullPath(fallbackDirectory), selected);
    }

    [TestMethod]
    public void AuditDataDirectoryFailsWhenNeitherLocationIsWritable()
    {
        Assert.Throws<UnauthorizedAccessException>(() => AuditDataDirectory.SelectWritableDirectory(
            Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "app"),
            Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "user"),
            _ => false));
    }

    [TestMethod]
    public void AuditDataDirectoryUsesConfiguredDirectoryWhenWritable()
    {
        var configuredDirectory = Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "configured");

        var selected = AuditDataDirectory.SelectConfiguredDirectory(configuredDirectory, _ => true);

        Assert.AreEqual(Path.GetFullPath(configuredDirectory), selected);
    }

    [TestMethod]
    public void AuditDataDirectoryRejectsUnwritableConfiguredDirectory()
    {
        Assert.Throws<UnauthorizedAccessException>(() => AuditDataDirectory.SelectConfiguredDirectory(
            Path.Combine(Path.GetTempPath(), "WindowsServiceAudit", "configured"),
            _ => false));
    }

    [TestMethod]
    public void ServicePathParserHandlesQuotedAndUnquotedCommands()
    {
        var quoted = ServicePathParser.Parse("\"C:\\Program Files\\Vendor\\service.exe\" -service");
        Assert.AreEqual("C:\\Program Files\\Vendor\\service.exe", quoted.ExecutablePath);
        Assert.IsFalse(quoted.UnquotedPathWithSpaces);

        var empty = ServicePathParser.Parse(string.Empty);
        Assert.AreEqual("No image path reported", empty.PathStatus);

        var malformed = ServicePathParser.Parse("\"C:\\Program Files\\Vendor\\service.exe");
        Assert.AreEqual("Malformed quoted image path", malformed.PathStatus);
    }

    [TestMethod]
    public void ServiceTargetUsesHostedDllAndHostExecutable()
    {
        var directory = CreateTempDirectory();
        try
        {
            var hostPath = Path.Combine(directory, "svchost.exe");
            var dllPath = Path.Combine(directory, "service.dll");
            File.WriteAllBytes(hostPath, []);
            File.WriteAllBytes(dllPath, []);

            var target = ServicePathParser.GetTarget("", $"\"{hostPath}\" -k TestGroup", dllPath);

            Assert.AreEqual($"{dllPath} (loaded by {hostPath})", target);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void CsvWriterUsesTheEstablishedTwentyFourColumnOrderAndEscapesValues()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "results.csv");
            var record = new ServiceAuditRecord
            {
                Status = "REVIEW",
                Name = "Service,One",
                DisplayName = "Example \"Service\"",
                WorkingSetMB = 12.5,
                State = "Running"
            };
            CsvReportWriter.Write(path, [record]);

            using var reader = new StreamReader(path);
            var header = reader.ReadLine();
            var data = reader.ReadLine();
            Assert.IsNotNull(header);
            Assert.IsNotNull(data);
            CollectionAssert.AreEqual(
                new[]
                {
                    "Status", "Name", "DisplayName", "WorkingSetMB", "State", "StartMode",
                    "DelayedAutoStart", "TriggerConfigurationPresent", "StartName", "ReviewSignals",
                    "ExecutablePath", "PathCandidates", "PathStatus", "UnquotedPathWithSpaces",
                    "ExecutableTarget", "CommandLine", "Description", "Category", "ClassificationEvidence",
                    "ProcessId", "Dependencies", "Dependents", "SignatureStatus", "Signer"
                },
                header.Split(',').Select(Unquote).ToArray());
            Assert.IsTrue(data.Contains("\"Service,One\"", StringComparison.Ordinal));
            Assert.IsTrue(data.Contains("\"Example \"\"Service\"\"\"", StringComparison.Ordinal));
            Assert.IsFalse(header.Contains("CpuPercent", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void CsvWriterPlacesReviewRecordsFirstThenSortsByDisplayName()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "results.csv");
            CsvReportWriter.Write(path,
            [
                new ServiceAuditRecord { Status = "OK", Name = "OkZ", DisplayName = "Zulu" },
                new ServiceAuditRecord { Status = "REVIEW", Name = "ReviewZ", DisplayName = "Zulu" },
                new ServiceAuditRecord { Status = "OK", Name = "OkA", DisplayName = "Alpha" },
                new ServiceAuditRecord { Status = "REVIEW", Name = "ReviewA", DisplayName = "Alpha" }
            ]);

            var names = File.ReadAllLines(path).Skip(1)
                .Select(line => Unquote(line.Split(',')[1])).ToArray();
            CollectionAssert.AreEqual(new[] { "ReviewA", "ReviewZ", "OkA", "OkZ" }, names);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void DocxReportWriterCreatesValidWordDocumentFromMarkdown()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "Win-Srv-Eval-Report.docx");
            const string report = """
                # Windows Service Audit Results

                - **Marked REVIEW:** 1
                - **Google Search:** [Search for Example Service](https://www.google.com/search?q=Example).

                ```text
                C:\Program Files\Vendor\service.exe
                ```
                """;

            DocxReportWriter.Write(path, report);

            using (var package = Package.Open(path, FileMode.Open, FileAccess.Read))
            {
                var relationship = package.GetRelationshipsByType(
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").Single();
                Assert.AreEqual("word/document.xml", relationship.TargetUri.OriginalString,
                    "OpenOffice requires a relative document relationship target.");
            }

            using var document = WordprocessingDocument.Open(path, false);
            var validationErrors = new OpenXmlValidator().Validate(document).ToArray();
            Assert.IsEmpty(validationErrors,
                string.Join(Environment.NewLine, validationErrors.Select(error => $"{error.Description} ({error.Path?.XPath})")));
            var mainPart = document.MainDocumentPart
                ?? throw new AssertFailedException("The generated DOCX has no main document part.");
            var body = mainPart.Document?.Body
                ?? throw new AssertFailedException("The generated DOCX has no document body.");
            var exportedText = string.Concat(body.Descendants<Text>().Select(text => text.Text));
            StringAssert.Contains(exportedText, "Windows Service Audit Results");
            StringAssert.Contains(exportedText, "Marked REVIEW:");
            StringAssert.Contains(exportedText, "Search for Example Service");
            StringAssert.Contains(exportedText, @"C:\Program Files\Vendor\service.exe");
            Assert.AreEqual(1, mainPart.HyperlinkRelationships.Count());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ResultsWriterReplacesOnlyGeneratedReviewBlock()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "Win-Srv-Eval-Report.md");
            File.WriteAllText(path, $"# User title{Environment.NewLine}Before{Environment.NewLine}{ResultsWriter.BeginMarker}{Environment.NewLine}old{Environment.NewLine}{ResultsWriter.EndMarker}{Environment.NewLine}After{Environment.NewLine}");
            var record = new ServiceAuditRecord
            {
                Status = "REVIEW", Name = "ReviewSvc", DisplayName = "Review Service",
                State = "Stopped", ExecutableTarget = @"C:\Windows\System32\test.dll"
            };

            ResultsWriter.Update(path, [record]);
            var output = File.ReadAllText(path);

            StringAssert.Contains(output, "# User title");
            StringAssert.Contains(output, "Before");
            StringAssert.Contains(output, "### Review Service (`ReviewSvc`)");
            StringAssert.Contains(output, "After");
            Assert.IsFalse(output.Contains("old", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ResultsWriterDoesNotOverwriteExistingUnmarkedResultsFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "Win-Srv-Eval-Report.md");
            const string original = "User-authored report with no app markers.";
            File.WriteAllText(path, original);

            Assert.Throws<InvalidDataException>(() => ResultsWriter.Update(path, []));

            Assert.AreEqual(original, File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ResultsWriterRegeneratesKnownReportsFromCurrentAuditRecords()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "Win-Srv-Eval-Report.md");
            File.WriteAllText(path, string.Join(Environment.NewLine,
            [
                "# Service Evaluation Summary",
                "",
                "The audit inventoried 295 services.",
                "",
                "### USBIP Device Host (`usbipd`)",
                "old static sample details",
                ResultsWriter.BeginMarker,
                "old USBIP review details",
                ResultsWriter.EndMarker
            ]));
            var records = new[]
            {
                new ServiceAuditRecord
                {
                    Status = "REVIEW", Name = "OtherSvc", DisplayName = "Other Service",
                    State = "Stopped", StartMode = "Manual", ExecutablePath = @"C:\Tools\other.exe",
                    ExecutableTarget = @"C:\Tools\other.exe", PathStatus = "File exists",
                    Category = "Third-party signer present; manual review", Signer = "Example Publisher",
                    SignatureStatus = "Signer certificate present; trust not evaluated",
                    Description = "A user-installed background helper."
                },
                new ServiceAuditRecord
                {
                    Status = "OK", Name = "WhitelistedSvc", DisplayName = "Whitelisted Service",
                    State = "Running"
                }
            };

            ResultsWriter.Update(path, records);
            var output = File.ReadAllText(path);

            StringAssert.Contains(output, "**Services inventoried:** 2");
            StringAssert.Contains(output, "**Marked REVIEW:** 1");
            StringAssert.Contains(output, "### Other Service (`OtherSvc`)");
            StringAssert.Contains(output, "**What this service does:** A user-installed background helper.");
            StringAssert.Contains(output, $"https://www.google.com/search?q={Uri.EscapeDataString("Other Service (OtherSvc)")}");
            Assert.IsFalse(output.Contains(ResultsWriter.BeginMarker, StringComparison.Ordinal));
            Assert.IsFalse(output.Contains(ResultsWriter.EndMarker, StringComparison.Ordinal));
            Assert.IsFalse(output.Contains("USBIP Device Host", StringComparison.Ordinal));
            Assert.IsFalse(output.Contains("old static sample details", StringComparison.Ordinal));
            Assert.IsFalse(output.Contains("295 services", StringComparison.Ordinal));
            Assert.IsFalse(output.Contains("Whitelisted Service", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void GeneratedReportContainsOnlyCurrentReviewServicesAndExplainsEmptyResults()
    {
        var output = ResultsWriter.GenerateReport(
        [
            new ServiceAuditRecord { Status = "OK", Name = "Allowed", DisplayName = "Allowed Service" }
        ],
        new DateTime(2026, 10, 29, 12, 0, 0));

        StringAssert.Contains(output, "Generated: 2026-10-29 12:00");
        StringAssert.Contains(output, "No services currently have `REVIEW` status.");
        StringAssert.Contains(output, "**Services inventoried:** 1");
        Assert.IsFalse(output.Contains("Allowed Service", StringComparison.Ordinal));
        StringAssert.Contains(output, "not safety or importance ratings");
    }

    [TestMethod]
    public void GoogleSearchLinksAreGeneratedFromEachReviewedServiceName()
    {
        var output = ResultsWriter.GenerateReviewSection(
        [
            new ServiceAuditRecord { Status = "REVIEW", Name = "VendorSvc", DisplayName = "Vendor Service" },
            new ServiceAuditRecord { Status = "REVIEW", Name = "DeviceHost", DisplayName = "Device Host" }
        ]);

        StringAssert.Contains(output, $"https://www.google.com/search?q={Uri.EscapeDataString("Vendor Service (VendorSvc)")}");
        StringAssert.Contains(output, $"https://www.google.com/search?q={Uri.EscapeDataString("Device Host (DeviceHost)")}");
        StringAssert.Contains(output, "[Vendor Service (`VendorSvc`)](");
        Assert.IsFalse(output.Contains("Search for ", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("USBIP", StringComparison.Ordinal));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "WindowsServiceAuditTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1].Replace("\"\"", "\"")
            : value;
    }
}