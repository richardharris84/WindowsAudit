using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsServiceAudit.Core;

namespace WindowsServiceAudit.App;

public sealed class WhitelistEditorWindow : Window
{
    private readonly string _whitelistPath;
    private readonly TextBox _termsTextBox;

    public int SavedTermCount { get; private set; }

    public WhitelistEditorWindow(string whitelistPath, IReadOnlyList<string> terms)
    {
        _whitelistPath = Path.GetFullPath(whitelistPath);
        Title = "Edit Whitelist";
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WindowsAudit;component/Assets/WindowsAudit.ico"));
        Width = 620;
        Height = 680;
        MinWidth = 480;
        MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(244, 247, 247));

        var layout = new DockPanel { Margin = new Thickness(20) };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var cancel = new Button
        {
            Content = "_Cancel",
            ToolTip = "Cancel (Alt+C or Escape)",
            IsCancel = true,
            MinWidth = 90,
            Margin = new Thickness(4, 0, 0, 0)
        };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button
        {
            Content = "_Save",
            ToolTip = "Save (Alt+S)",
            MinWidth = 90,
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(8, 126, 131)),
            Foreground = Brushes.White,
            IsDefault = true
        };
        save.Click += Save_Click;
        actions.Children.Add(cancel);
        actions.Children.Add(save);
        DockPanel.SetDock(actions, Dock.Bottom);
        layout.Children.Add(actions);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var termsLabel = new Label
        {
            Content = "Whitelist _terms",
            Padding = new Thickness(0),
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(28, 48, 56)),
            Margin = new Thickness(0, 0, 0, 6)
        };
        content.Children.Add(termsLabel);

        var description = new TextBlock
        {
            Text = "Enter one match term per line. Terms are matched as case-insensitive substrings.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 116, 123)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(description, 1);
        content.Children.Add(description);

        _termsTextBox = new TextBox
        {
            Text = string.Join(Environment.NewLine, terms),
            AcceptsReturn = true,
            AcceptsTab = false,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(10),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(215, 225, 226))
        };
        termsLabel.Target = _termsTextBox;
        AutomationProperties.SetName(_termsTextBox, "Whitelist terms");
        AutomationProperties.SetLabeledBy(_termsTextBox, termsLabel);
        AutomationProperties.SetHelpText(_termsTextBox, description.Text);
        Grid.SetRow(_termsTextBox, 2);
        content.Children.Add(_termsTextBox);

        layout.Children.Add(content);
        Content = layout;
        Accessibility.Configure(this, layout);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var terms = _termsTextBox.Text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (terms.Length == 0)
        {
            MessageBox.Show(this, "Enter at least one whitelist term before saving.", "Whitelist is empty", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Whitelist.Save(_whitelistPath, terms);
            SavedTermCount = terms.Length;
            MessageBox.Show(this, $"Whitelist saved to:\n{_whitelistPath}", "Whitelist saved", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            MessageBox.Show(this, exception.Message, "Could not save whitelist", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}