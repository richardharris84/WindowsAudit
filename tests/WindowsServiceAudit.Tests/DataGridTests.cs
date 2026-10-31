using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WindowsServiceAudit.App;
using WindowsServiceAudit.Core;
using AccessibilityMode = WindowsServiceAudit.App.Accessibility;

namespace WindowsServiceAudit.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DataGridTests
{
    [STATestMethod]
    public void WindowsPreserveGridBehaviorAndExposeKeyboardAccess()
    {
        var app = new WindowsServiceAudit.App.App();
        app.InitializeComponent();
        var window = new MainWindow();
        try
        {
            Assert.IsFalse(AccessibilityMode.Enabled);
            var toggle = (MenuItem)window.FindName("AccessibilityMenuItem");
            Assert.IsFalse(toggle.IsChecked);
            var initialButtons = Descendants(window).OfType<Button>().ToArray();
            Assert.IsTrue(initialButtons.All(button => !((string)button.Content).Contains('_')));
            var originalBackground = window.Background;
            AccessibilityMode.SetEnabled(true);
            toggle.IsChecked = true;
            var grid = (DataGrid)window.FindName("ServicesGrid");
            var records = new[]
            {
                new ServiceAuditRecord { Name = "Large", WorkingSetMB = 100 },
                new ServiceAuditRecord { Name = "Small", WorkingSetMB = 9 }
            };
            grid.ItemsSource = records;
            grid.Measure(new Size(1500, 600));
            grid.Arrange(new Rect(0, 0, 1500, 600));
            grid.UpdateLayout();

            var expectedColumns = typeof(ServiceAuditRecord).GetProperties()
                .Select(property => property.Name)
                .Except([nameof(ServiceAuditRecord.UnquotedPathWithSpaces), nameof(ServiceAuditRecord.Dependents)])
                .ToArray();
            CollectionAssert.AreEquivalent(expectedColumns, grid.Columns.Select(column => column.SortMemberPath).ToArray());
            var memoryColumn = grid.Columns.Single(column => column.SortMemberPath == nameof(ServiceAuditRecord.WorkingSetMB));
            Assert.AreEqual("Memory (MB)", memoryColumn.Header);
            Assert.IsTrue(memoryColumn.CanUserSort);
            Assert.IsTrue(grid.IsReadOnly);

            var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
            view.SortDescriptions.Add(new SortDescription(memoryColumn.SortMemberPath, ListSortDirection.Ascending));
            CollectionAssert.AreEqual(new[] { "Small", "Large" }, view.Cast<ServiceAuditRecord>().Select(record => record.Name).ToArray());

            var controls = Descendants(window).ToArray();
            var menu = controls.OfType<Menu>().Single();
            AssertMenuAccessKeys(menu.Items);
            var buttons = controls.OfType<Button>().ToArray();
            Assert.HasCount(4, buttons);
            CollectionAssert.AreEquivalent(
                new[] { 's', 'w', 'e', 'g' },
                buttons.Select(button => AccessKey((string)button.Content)).ToArray());
            Assert.IsTrue(buttons.All(button => !string.IsNullOrWhiteSpace(AutomationProperties.GetName(button))));
            Assert.AreEqual("Windows services inventory", AutomationProperties.GetName(grid));

            var filter = (TextBox)window.FindName("FilterTextBox");
            var sample = (ComboBox)window.FindName("SampleSecondsComboBox");
            Assert.AreSame(filter, ((Label)window.FindName("FilterLabel")).Target);
            Assert.AreSame(sample, ((Label)window.FindName("SampleSecondsLabel")).Target);
            Assert.AreEqual("Filter services", AutomationProperties.GetName(filter));
            Assert.AreEqual("Sample interval in seconds", AutomationProperties.GetName(sample));

            var status = (TextBlock)window.FindName("StatusText");
            Assert.AreEqual(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(status));
            Assert.IsNotNull(buttons[0].FocusVisualStyle);
            Assert.AreEqual(SystemColors.WindowBrush, window.Background);
            Assert.AreEqual(SystemColors.WindowTextBrush, window.Foreground);

            var increase = controls.OfType<MenuItem>().Single(item => (string)item.Header == "_Increase text size");
            var decrease = controls.OfType<MenuItem>().Single(item => (string)item.Header == "_Decrease text size");
            var reset = controls.OfType<MenuItem>().Single(item => (string)item.Header == "_Normal text size");
            var layout = (DockPanel)window.FindName("AccessibleLayout");
            for (var index = 0; index < 5; index++)
                increase.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.AreEqual(2.0, ((ScaleTransform)layout.LayoutTransform).ScaleX);
            var scroll = (ScrollViewer)window.Content;
            scroll.ApplyTemplate();
            scroll.Measure(new Size(750, 400));
            scroll.Arrange(new Rect(0, 0, 750, 400));
            scroll.UpdateLayout();
            Assert.AreEqual(ScrollBarVisibility.Auto, scroll.HorizontalScrollBarVisibility);
            Assert.IsGreaterThan(scroll.ViewportWidth, scroll.ExtentWidth);
            Assert.IsGreaterThanOrEqualTo(140.0, filter.ActualWidth);
            decrease.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.AreEqual(1.75, ((ScaleTransform)layout.LayoutTransform).ScaleX);
            AssertDialogKeyboardAccess();
            reset.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.AreEqual(1.0, ((ScaleTransform)layout.LayoutTransform).ScaleX);
            AccessibilityMode.SetEnabled(false);
            toggle.IsChecked = false;
            Assert.AreEqual(originalBackground, window.Background);
            Assert.IsTrue(initialButtons.All(button => !((string)button.Content).Contains('_')));
            Assert.AreEqual(AutomationLiveSetting.Off, AutomationProperties.GetLiveSetting(status));
            Assert.DoesNotContain("Alt+", (string)filter.ToolTip);
            AssertMenuAccessKeys(menu.Items);
            AccessibilityMode.SetEnabled(true);
            Assert.IsTrue(initialButtons.All(button => ((string)button.Content).Contains('_')));
        }
        finally
        {
            AccessibilityMode.SetEnabled(false);
            window.Close();
            app.Shutdown();
        }
    }

    [TestMethod]
    public void AccessibilityPreferenceDefaultsOffAndRoundTrips()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"WindowsAudit-accessibility-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "accessibility.json");
        try
        {
            Assert.IsFalse(AccessibilitySettings.Load(path));
            AccessibilitySettings.Save(path, true);
            Assert.IsTrue(AccessibilitySettings.Load(path));
            AccessibilitySettings.Save(path, false);
            Assert.IsFalse(AccessibilitySettings.Load(path));
            File.WriteAllText(path, "invalid");
            Assert.Throws<System.Text.Json.JsonException>(() => AccessibilitySettings.Load(path));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertDialogKeyboardAccess()
    {
        var editor = new WhitelistEditorWindow(Path.Combine(Path.GetTempPath(), "whitelist.csv"), ["test"]);
        var readme = new ReadmeWindow("# Test", AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "README.md"), showDocxExport: true);
        try
        {
            var editorControls = Descendants(editor).ToArray();
            var editorLayout = (FrameworkElement)((ScrollViewer)editor.Content).Content;
            Assert.AreEqual(1.75, ((ScaleTransform)editorLayout.LayoutTransform).ScaleX);
            var terms = editorControls.OfType<TextBox>().Single();
            Assert.IsFalse(terms.AcceptsTab);
            Assert.IsTrue(terms.AcceptsReturn);
            Assert.AreSame(terms, editorControls.OfType<Label>().Single().Target);
            Assert.AreEqual("Whitelist terms", AutomationProperties.GetName(terms));
            var editorButtons = editorControls.OfType<Button>().ToArray();
            CollectionAssert.AreEquivalent(new[] { 'c', 's' },
                editorButtons.Select(button => AccessKey((string)button.Content)).ToArray());
            Assert.IsTrue(editorButtons.Single(button => AccessKey((string)button.Content) == 'c').IsCancel);

            var readmeButtons = Descendants(readme).OfType<Button>().ToArray();
            CollectionAssert.AreEquivalent(new[] { 'e', 'o' },
                readmeButtons.Select(button => AccessKey((string)button.Content)).ToArray());
            var close = readmeButtons.Single(button => AccessKey((string)button.Content) == 'o');
            Assert.IsTrue(close.IsDefault);
            Assert.IsTrue(close.IsCancel);
            Assert.IsInstanceOfType<DockPanel>(readme.Content);
            var documentViewer = Descendants(readme).OfType<FlowDocumentScrollViewer>().Single();
            Assert.AreEqual(ScrollBarVisibility.Disabled, documentViewer.HorizontalScrollBarVisibility);
            var readmeLayout = (DockPanel)readme.Content;
            Assert.AreEqual(1.75, ((ScaleTransform)readmeLayout.LayoutTransform).ScaleX);
            readmeLayout.Measure(new Size(560, 440));
            readmeLayout.Arrange(new Rect(0, 0, 560, 440));
            readmeLayout.UpdateLayout();
            Assert.IsGreaterThan(0.0, documentViewer.ActualWidth);
            Assert.IsLessThanOrEqualTo(560.0, documentViewer.ActualWidth * 1.75);
        }
        finally
        {
            editor.Close();
            readme.Close();
        }
    }

    private static void AssertMenuAccessKeys(ItemCollection items)
    {
        var menus = items.OfType<MenuItem>().ToArray();
        var keys = menus.Select(item => AccessKey((string)item.Header)).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct().Count(), "Menu siblings must have unique access keys.");
        foreach (var item in menus)
            AssertMenuAccessKeys(item.Items);
    }

    private static char AccessKey(string text)
    {
        var index = text.IndexOf('_');
        Assert.IsTrue(index >= 0 && index < text.Length - 1, $"Missing access key: {text}");
        return char.ToLowerInvariant(text[index + 1]);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}