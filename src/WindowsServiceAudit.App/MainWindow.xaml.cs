using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using WindowsServiceAudit.Core;

namespace WindowsServiceAudit.App;

public partial class MainWindow : Window
{
    private readonly AuditRunner _auditRunner = new();
    private readonly ObservableCollection<ServiceAuditRecord> _services = [];
    private ICollectionView? _servicesView;
    private bool _welcomeShown;
    private string? _configuredAuditDirectory;

    public MainWindow()
    {
        InitializeComponent();
        Accessibility.Configure(this, AccessibleLayout, minimumWidth: 1080);
        ServicesGrid.ItemsSource = _services;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var enabled = AccessibilitySettings.Load();
            AccessibilityMenuItem.IsChecked = enabled;
            Accessibility.SetEnabled(enabled);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show(this, exception.Message, "Accessibility preference unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _servicesView = CollectionViewSource.GetDefaultView(_services);
        _servicesView.Filter = FilterService;
        try
        {
            _configuredAuditDirectory = AuditFolderSettings.Load();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Audit folder setting unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        ShowReadme(isStartup: true);
    }

    private void FilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _servicesView?.Refresh();
    }

    private async void RunAudit_Click(object sender, RoutedEventArgs e)
    {
        string outputDirectory;
        try
        {
            outputDirectory = GetAuditDataDirectory();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Audit folder unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SetBusy(true, "Starting local Windows service inventory…");
        ViewResultsMenuItem.IsEnabled = false;
        string? reportPath = null;
        try
        {
            var sampleSeconds = int.TryParse((SampleSecondsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var selectedSeconds)
                ? selectedSeconds
                : 1;
            var progress = new Progress<AuditProgress>(UpdateProgress);
            var results = await _auditRunner.RunAsync(outputDirectory, sampleSeconds, progress);
            _services.Clear();
            foreach (var record in results) _services.Add(record);
            _servicesView?.Refresh();
            TotalCountText.Text = results.Count.ToString("N0");
            ReviewCountText.Text = results.Count(record => record.Status == "REVIEW").ToString("N0");
            AnnounceStatus($"Audit complete. {results.Count:N0} services; {results.Count(record => record.Status == "REVIEW"):N0} require review.");
            reportPath = Path.Combine(outputDirectory, "Win-Srv-Eval-Report.md");
            ViewResultsMenuItem.IsEnabled = true;
        }
        catch (Exception exception)
        {
            AnnounceStatus("Audit failed.");
            MessageBox.Show(this, exception.Message, "Audit failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }

        if (reportPath is not null)
        {
            try
            {
                ShowResults(outputDirectory, reportPath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Could not open audit report", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ViewResults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var outputDirectory = GetAuditDataDirectory();
            var reportPath = Path.Combine(outputDirectory, "Win-Srv-Eval-Report.md");
            if (!File.Exists(reportPath))
            {
                MessageBox.Show(this, "Run an audit before viewing results.", "No audit results", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowResults(outputDirectory, reportPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Could not open audit report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowResults(string outputDirectory, string reportPath)
    {
        var report = File.ReadAllText(reportPath);
        var dialog = new ReadmeWindow(report, outputDirectory, reportPath, "Windows Audit — Results", showDocxExport: true) { Owner = this };
        dialog.ShowDialog();
    }

    private void EditWhitelist_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var outputDirectory = GetAuditDataDirectory();
            var whitelistPath = Path.Combine(outputDirectory, "whitelist.csv");
            var terms = Whitelist.LoadOrSeed(outputDirectory);
            var editor = new WhitelistEditorWindow(whitelistPath, terms) { Owner = this };
            if (editor.ShowDialog() == true)
                StatusText.Text = $"Whitelist saved · {editor.SavedTermCount:N0} terms · {whitelistPath}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Whitelist unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfigureAuditFolder_Click(object sender, RoutedEventArgs e)
    {
        string initialDirectory;
        try
        {
            initialDirectory = GetAuditDataDirectory();
        }
        catch (Exception)
        {
            initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        var picker = new OpenFolderDialog
        {
            Title = "Choose the audit output folder",
            InitialDirectory = initialDirectory,
            Multiselect = false
        };
        if (picker.ShowDialog(this) != true) return;

        try
        {
            var selectedDirectory = AuditDataDirectory.Resolve(
                AppContext.BaseDirectory,
                GetFallbackAuditDirectory(),
                picker.FolderName);
            AuditFolderSettings.Save(selectedDirectory);
            _configuredAuditDirectory = selectedDirectory;
            StatusText.Text = $"Audit folder configured · {selectedDirectory}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Could not configure audit folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_services.Count == 0)
        {
            MessageBox.Show(this, "Run an audit before exporting results.", "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export service inventory",
            InitialDirectory = AppContext.BaseDirectory,
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = "Win-Srv-Eval-Results.csv"
        };

        if (dialog.ShowDialog(this) != true) return;
        try
        {
            CsvReportWriter.Write(dialog.FileName, _services);
            StatusText.Text = $"Exported {_services.Count:N0} services · {dialog.FileName}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GettingStarted_Click(object sender, RoutedEventArgs e) => ShowReadme(isStartup: false);

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void OpenServices_Click(object sender, RoutedEventArgs e) =>
        OpenWindowsUtility("services.msc", "Could not open Services.");

    private void OpenInstalledApps_Click(object sender, RoutedEventArgs e) =>
        OpenWindowsUtility("ms-settings:appsfeatures", "Could not open Installed apps.");

    private void OpenStartupApps_Click(object sender, RoutedEventArgs e) =>
        OpenWindowsUtility("ms-settings:startupapps", "Could not open Startup Apps.");

    private async void CleanupLargeFiles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await OpenExplorerLargeFileSearchAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or TimeoutException or COMException)
        {
            MessageBox.Show(this, $"Could not open the large-file search in File Explorer.\n\n{exception.Message}", "Could not open Windows utility", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static async Task OpenExplorerLargeFileSearchAsync()
    {
        const string query = "size:>10MB";
        var existingWindows = GetExplorerWindows()
            .Select(window => window.Current.NativeWindowHandle)
            .ToHashSet();
        var hasCDrive = DriveInfo.GetDrives().Any(drive =>
            string.Equals(drive.Name, @"C:\", StringComparison.OrdinalIgnoreCase));
        var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        var explorerLocation = hasCDrive ? @"C:\" : "shell:MyComputerFolder";
        var startInfo = new ProcessStartInfo(explorerPath) { UseShellExecute = false };
        startInfo.ArgumentList.Add($"/n,{explorerLocation}");
        Process.Start(startInfo);

        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < timeout)
        {
            foreach (var window in GetExplorerWindows())
            {
                if (existingWindows.Contains(window.Current.NativeWindowHandle)) continue;
                var searchBox = FindExplorerSearchBox(window);
                if (searchBox is null) continue;
                if (!searchBox.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
                    throw new InvalidOperationException("File Explorer's Search box does not support text input.");

                searchBox.SetFocus();
                ((ValuePattern)valuePattern).SetValue(query);
                searchBox.SetFocus();
                SendEnterKey();
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("File Explorer's Search box did not become available.");
    }

    private static IEnumerable<AutomationElement> GetExplorerWindows()
    {
        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
        foreach (AutomationElement window in windows)
        {
            if (string.Equals(window.Current.ClassName, "CabinetWClass", StringComparison.Ordinal))
                yield return window;
        }
    }

    private static AutomationElement? FindExplorerSearchBox(AutomationElement window)
    {
        var edits = window.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
        foreach (AutomationElement edit in edits)
        {
            if (edit.Current.Name.Trim().StartsWith("Search ", StringComparison.OrdinalIgnoreCase))
                return edit;
        }

        return null;
    }

    private static void SendEnterKey()
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable to submit the Explorer search.");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create the Windows Script Host shell.");
        try
        {
            shellType.InvokeMember("SendKeys", System.Reflection.BindingFlags.InvokeMethod, null, shell, ["{ENTER}"]);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    private void OpenWindowsUtility(string target, string errorMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, $"{errorMessage}\n\n{exception.Message}", "Could not open Windows utility", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetAuditDataDirectory()
    {
        return AuditDataDirectory.Resolve(AppContext.BaseDirectory, GetFallbackAuditDirectory(), _configuredAuditDirectory);
    }

    private static string GetFallbackAuditDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsServiceAudit");

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private bool FilterService(object item)
    {
        if (item is not ServiceAuditRecord service) return false;
        var filter = FilterTextBox?.Text.Trim();
        if (string.IsNullOrWhiteSpace(filter)) return true;

        return new[]
        {
            service.Status, service.Name, service.DisplayName, service.State, service.StartMode,
            service.ExecutablePath, service.ExecutableTarget, service.Category, service.ReviewSignals,
            service.Description, service.Signer
        }.Any(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private void ServicesGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName is nameof(ServiceAuditRecord.UnquotedPathWithSpaces) or nameof(ServiceAuditRecord.Dependents))
        {
            e.Cancel = true;
            return;
        }

        e.Column.Header = e.PropertyName switch
        {
            nameof(ServiceAuditRecord.WorkingSetMB) => "Memory (MB)",
            nameof(ServiceAuditRecord.StartName) => "Service Account",
            nameof(ServiceAuditRecord.ExecutablePath) => "Executable Path",
            nameof(ServiceAuditRecord.ExecutableTarget) => "Executable Target",
            nameof(ServiceAuditRecord.PathCandidates) => "Path Candidates",
            nameof(ServiceAuditRecord.PathStatus) => "Path Status",
            nameof(ServiceAuditRecord.DelayedAutoStart) => "Delayed Auto Start",
            nameof(ServiceAuditRecord.TriggerConfigurationPresent) => "Trigger Configuration",
            nameof(ServiceAuditRecord.ClassificationEvidence) => "Classification Evidence",
            nameof(ServiceAuditRecord.ProcessId) => "Process ID",
            nameof(ServiceAuditRecord.ReviewSignals) => "Review Signals",
            nameof(ServiceAuditRecord.SignatureStatus) => "Signature Status",
            _ => e.PropertyName
        };
        if (e.PropertyName == nameof(ServiceAuditRecord.WorkingSetMB))
        {
            e.Column.SortMemberPath = nameof(ServiceAuditRecord.WorkingSetMB);
            e.Column.CanUserSort = true;
        }
        e.Column.MinWidth = 90;
        e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
    }

    private void ShowReadme(bool isStartup)
    {
        if (isStartup)
        {
            if (_welcomeShown) return;
            _welcomeShown = true;
        }
        var assetsRoot = Path.Combine(AppContext.BaseDirectory, "Assets");
        var readmePath = Path.Combine(assetsRoot, "README.md");
        var text = File.Exists(readmePath)
            ? File.ReadAllText(readmePath)
            : "Windows Service Audit\n\nREADME.md was not found beside the application.";
        var dialog = new ReadmeWindow(text, assetsRoot, readmePath) { Owner = this };
        dialog.ShowDialog();
    }

    private void SetBusy(bool busy, string message)
    {
        AuditProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = message;
        IsEnabled = !busy;
        if (busy) AnnounceStatus(message);
    }

    private void IncreaseText_Click(object sender, RoutedEventArgs e) => Accessibility.ChangeScale(0.25);
    private void DecreaseText_Click(object sender, RoutedEventArgs e) => Accessibility.ChangeScale(-0.25);
    private void ResetText_Click(object sender, RoutedEventArgs e) => Accessibility.ChangeScale(null);

    private void AnnounceStatus(string message)
    {
        StatusText.Text = message;
        if (!Accessibility.Enabled) return;
        var peer = UIElementAutomationPeer.FromElement(StatusText) ?? UIElementAutomationPeer.CreatePeerForElement(StatusText);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void Accessibility_Click(object sender, RoutedEventArgs e)
    {
        var enabled = AccessibilityMenuItem.IsChecked;
        try
        {
            AccessibilitySettings.Save(enabled);
            Accessibility.SetEnabled(enabled);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AccessibilityMenuItem.IsChecked = Accessibility.Enabled;
            MessageBox.Show(this, exception.Message, "Could not save accessibility preference", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateProgress(AuditProgress progress)
    {
        StatusText.Text = progress.Total > 0
            ? $"{progress.Message} ({progress.Completed:N0}/{progress.Total:N0})"
            : progress.Message;
    }
}