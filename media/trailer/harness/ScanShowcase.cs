using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsServiceAudit.App;
using WindowsServiceAudit.Core;

internal static partial class Program
{
    private static void ScanShowcase(App app)
    {
        var window = new MainWindow();
        var content = (FrameworkElement)window.Content;
        window.Content = null;
        var host = new Border { Child = content, Background = Brushes.White, Width = 1600, Height = 800 };
        var grid = (DataGrid)window.FindName("ServicesGrid");
        var records = (ObservableCollection<ServiceAuditRecord>)grid.ItemsSource;
        var filter = (TextBox)window.FindName("FilterTextBox");
        var view = CollectionViewSource.GetDefaultView(records);
        typeof(MainWindow).GetField("_servicesView", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, view);
        var actualFilter = typeof(MainWindow).GetMethod("FilterService", BindingFlags.Instance | BindingFlags.NonPublic)!;
        view.Filter = item => (bool)actualFilter.Invoke(window, [item])!;

        void Layout()
        {
            host.Measure(new Size(1600, 800));
            host.Arrange(new Rect(0, 0, 1600, 800));
            host.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            host.UpdateLayout();
        }
        void Frame(string name, string title, string caption, Action<DrawingContext>? overlay = null)
        {
            Layout();
            Save(name, drawing =>
            {
                Text(drawing, title, 72, 35, 38, Ink, true);
                drawing.DrawRectangle(new VisualBrush(host) { AutoLayoutContent = false }, null, new Rect(160, 115, 1600, 800));
                overlay?.Invoke(drawing);
                Text(drawing, caption, 72, 950, 27, Ink);
                Footer(drawing, "SIMULATED SCAN • FICTIONAL SERVICE DATA • NO SETTINGS CHANGED");
            });
        }
        void Highlight(DrawingContext drawing, FrameworkElement element)
        {
            var origin = element.TranslatePoint(new Point(), content);
            drawing.DrawRoundedRectangle(null, new Pen(Teal, 3),
                new Rect(160 + origin.X - 4, 115 + origin.Y - 4, element.ActualWidth + 8, element.ActualHeight + 8), 5, 5);
        }

        var workspace = Path.Combine(AppContext.BaseDirectory, "Assets");
        var readmePath = Path.Combine(workspace, "README.md");
        var about = new ReadmeWindow(File.ReadAllText(readmePath), workspace, readmePath);
        var aboutContent = (FrameworkElement)about.Content;
        about.Content = null;
        var aboutHost = new Border { Child = aboutContent, Background = about.Background, Width = 1120, Height = 730 };
        aboutHost.Measure(new Size(1120, 730));
        aboutHost.Arrange(new Rect(0, 0, 1120, 730));
        aboutHost.UpdateLayout();
        Frame("startup-about", "Welcome to Windows Audit", "Start with About: what the app does, how to scan, and how to read results.", drawing =>
        {
            drawing.DrawRectangle(new SolidColorBrush(Color.FromArgb(95, 28, 48, 56)), null, new Rect(160, 115, 1600, 800));
            drawing.DrawRectangle(Brushes.White, new Pen(Teal, 1), new Rect(400, 139, 1120, 762));
            Text(drawing, about.Title, 416, 143, 17, Ink, true);
            drawing.DrawRectangle(new VisualBrush(aboutHost) { AutoLayoutContent = false }, null, new Rect(400, 171, 1120, 730));
        });
        aboutHost.Child = null;
        about.Close();

        var scanButton = Descendants(content).OfType<Button>().Single(button =>
            button.Content?.ToString()?.Contains("Scan Windows Services", StringComparison.Ordinal) == true);
        Frame("scan-ready", "Scan Windows Services", "Choose Scan Windows Services to inspect the local service inventory.",
            drawing => Highlight(drawing, scanButton));
        var sample = (ComboBox)window.FindName("SampleSecondsComboBox");
        Frame("scan-sample", "Choose a memory sampling delay", "Choose how long to wait before capturing a process memory snapshot.",
            drawing => Highlight(drawing, sample));
        var progress = (ProgressBar)window.FindName("AuditProgressBar");
        var status = (TextBlock)window.FindName("StatusText");
        progress.Visibility = Visibility.Visible;
        progress.IsIndeterminate = false;
        progress.Minimum = 0;
        progress.Maximum = 4;
        var messages = new[]
        {
            "Reading the local Windows service inventory…",
            "Waiting 1 second(s) before taking process working-set readings…",
            "Capturing process working sets… (6/12)",
            "Writing CSV and RESULTS report… (12/12)"
        };
        var captions = new[]
        {
            "Read service names, startup settings, executable paths, and account details.",
            "Allow the selected delay before measuring process memory.",
            "Capture a point-in-time memory reading. Shared hosts share a reading.",
            "Prepare the service inventory and detailed report locally."
        };
        for (var stage = 0; stage < messages.Length; stage++)
        {
            status.Text = messages[stage];
            progress.Value = stage + 1;
            Frame($"scan-phase-{stage}", "Scan Windows Services", captions[stage], drawing => Highlight(drawing, status));
        }
        progress.Visibility = Visibility.Collapsed;
        var names = new[] { "Example Cloud Sync", "Example Software Updater", "Demo Audio Service", "Demo Backup Agent",
            "Demo Bluetooth Support", "Demo Device Manager", "Demo Event Service", "Demo Network Helper", "Demo Print Service",
            "Demo Search Indexer", "Demo Security Service", "Demo Time Service" };
        var ids = new[] { "ExampleSync", "ExampleUpdater", "DemoAudio", "DemoBackup", "DemoBluetooth", "DemoDevice",
            "DemoEvent", "DemoNetwork", "DemoPrint", "DemoSearch", "DemoSecurity", "DemoTime" };
        var memory = new[] { 42.6, 12.4, 18.5, 84.2, 9.3, 7.8, 16.9, 23.1, 11.6, 65.7, 106.3, 4.2 };
        for (var row = 0; row < names.Length; row++)
        {
            var path = row < 2 ? $@"C:\Program Files\Example\{ids[row]}.exe" : $@"C:\Windows\System32\{ids[row]}.exe";
            records.Add(new ServiceAuditRecord
            {
                Name = ids[row], DisplayName = names[row], Status = row < 2 ? "REVIEW" : "OK",
                WorkingSetMB = memory[row], State = "Running", StartMode = row is 1 or 4 or 8 or 11 ? "Manual" : "Auto",
                StartName = "LocalSystem", ExecutablePath = path, ExecutableTarget = path,
                PathStatus = "Demo data; not checked", SignatureStatus = "Demo data; not checked",
                Description = "Fictional example service used to demonstrate Windows Audit.",
                Category = row < 2 ? "Demo service; manual review" : "Demo service", ProcessId = 1000 + row
            });
        }
        var allRecords = records.ToArray();
        ((TextBlock)window.FindName("TotalCountText")).Text = "12";
        ((TextBlock)window.FindName("ReviewCountText")).Text = "2";
        ((MenuItem)window.FindName("ViewResultsMenuItem")).IsEnabled = true;
        status.Text = "Demo scan complete. Fictional service data.";
        Frame("scan-complete", "Your service inventory, ready to explore",
            "See status, service name, memory, startup mode, account, and executable path.");
        for (var count = 1; count <= 6; count++)
        {
            filter.Text = "REVIEW"[..count];
            Frame($"scan-filter-{count}", "Focus on services that need investigation",
                "REVIEW means a whitelist mismatch—not a safety verdict.", drawing => Highlight(drawing, filter));
        }
        if (grid.Items.Count != 2) throw new InvalidOperationException("REVIEW filter did not show exactly two fictional services.");
        filter.Text = "";
        var memoryColumn = grid.Columns.Single(column => column.SortMemberPath == nameof(ServiceAuditRecord.WorkingSetMB));
        var sort = typeof(DataGrid).GetMethod("PerformSort", BindingFlags.Instance | BindingFlags.NonPublic)!;
        sort.Invoke(grid, [memoryColumn]);
        sort.Invoke(grid, [memoryColumn]);
        if (grid.Items.Cast<ServiceAuditRecord>().First().WorkingSetMB != 106.3)
            throw new InvalidOperationException("Memory sorting did not put the largest value first.");
        Frame("scan-memory", "Compare memory snapshots",
            "Sort Memory (MB) numerically to bring the largest process snapshots to the top.");
        grid.SelectedItem = allRecords[0];
        Frame("scan-details", "Investigate the software behind a service",
            "Check its executable path, startup mode, and account before deciding what to do.", drawing =>
        {
            drawing.DrawRoundedRectangle(Brushes.White, new Pen(Teal, 2), new Rect(420, 660, 1080, 195), 10, 10);
            Text(drawing, allRecords[0].DisplayName + "  •  " + allRecords[0].Status, 450, 680, 27, Ink, true);
            Text(drawing, allRecords[0].ExecutablePath, 450, 730, 25, Teal, true);
            Text(drawing, "Startup: Auto    Account: LocalSystem    Memory snapshot: 42.6 MB", 450, 780, 23, Ink);
        });
        var report = ResultsWriter.GenerateReport(allRecords, new DateTime(2026, 10, 30, 12, 0, 0));
        var reportPath = Path.Combine(Root, "Win-Srv-Eval-Report.md");
        var reportWindow = new ReadmeWindow(report, Root, reportPath, showDocxExport: true);
        var reportContent = (FrameworkElement)reportWindow.Content;
        reportWindow.Content = null;
        host.Child = reportContent;
        Frame("scan-report", "Keep a detailed report",
            "Review the summary and service details. Export the report to DOCX when needed.");
        host.Child = null;
        reportWindow.Close();
        window.Close();
        app.Shutdown();
        Console.WriteLine("Rendered startup About and expanded scan showcase; verified filtering and numeric sorting.");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
