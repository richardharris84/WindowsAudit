using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WindowsServiceAudit.App;
using WindowsServiceAudit.Core;

namespace WindowsServiceAudit.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DataGridTests
{
    [STATestMethod]
    public void GridHidesOnlyUnquotedPathAndDependentsAndPreservesMemorySorting()
    {
        var app = new WindowsServiceAudit.App.App();
        app.InitializeComponent();
        var window = new MainWindow();
        try
        {
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
        }
        finally
        {
            window.Close();
            app.Shutdown();
        }
    }
}