using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace WindowsServiceAudit.App;

public sealed class AboutWindow : Window
{
    internal const string RepositoryUrl = "https://github.com/richardharris84/WindowsAudit";
    internal string AboutText { get; }

    public AboutWindow()
    {
        var version = typeof(MainWindow).Assembly.GetName().Version
            ?? throw new InvalidOperationException("Application assembly version is unavailable.");
        var informationText = $"Windows Audit\nVersion {version.ToString(4)}\n\nBuilt by Richard Harris\n\nOpen-source GitHub repository:\n";
        AboutText = informationText + RepositoryUrl;
        Title = "Windows Audit";
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WindowsAudit;component/Assets/WindowsAudit.ico"));
        Width = 560;
        Height = 340;
        MinWidth = 400;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var layout = new DockPanel { Margin = new Thickness(20) };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var copy = new Button { Content = "_Copy", MinWidth = 90, ToolTip = "Copy application information (Alt+C)" };
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(AboutText); }
            catch (ExternalException exception)
            {
                MessageBox.Show(this, exception.Message, "Could not copy application information", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        var close = new Button
        {
            Content = "_OK",
            MinWidth = 90,
            Margin = new Thickness(8, 0, 0, 0),
            IsDefault = true,
            IsCancel = true
        };
        close.Click += (_, _) => Close();
        actions.Children.Add(copy);
        actions.Children.Add(close);
        DockPanel.SetDock(actions, Dock.Bottom);
        layout.Children.Add(actions);

        var information = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };
        information.Inlines.Add(informationText);
        var link = new Hyperlink { NavigateUri = new Uri(RepositoryUrl) };
        link.Inlines.Add(RepositoryUrl);
        link.RequestNavigate += OpenRepository;
        information.Inlines.Add(link);
        layout.Children.Add(new ScrollViewer
        {
            Content = information,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });
        Content = layout;
        Accessibility.Configure(this, layout, horizontalScrolling: false);
    }

    private void OpenRepository(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true }); }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            MessageBox.Show(this, exception.Message, "Could not open GitHub repository", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }
}
