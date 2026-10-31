using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowsServiceAudit.Core;

namespace WindowsServiceAudit.App;

public sealed class ReadmeWindow : Window
{
    private static readonly Regex HeadingPattern = new(@"^(#{1,6})\s+(.+?)\s*#*\s*$", RegexOptions.Compiled);
    private static readonly Regex ListPattern = new(@"^\s*(?<marker>[-*+]|\d+\.)\s+(?<text>.+)$", RegexOptions.Compiled);
    private static readonly Regex HorizontalRulePattern = new(@"^ {0,3}(?:(?:-[ \t]*){3,}|(?:\*[ \t]*){3,}|(?:_[ \t]*){3,})$", RegexOptions.Compiled);
    private static readonly Regex InlinePattern = new(@"(?<code>`[^`]+`)|(?<bold>\*\*.+?\*\*)|(?<italic>\*[^*]+\*)|(?<link>\[[^\]]+\]\([^)]+\))", RegexOptions.Compiled);
    private readonly string _assetsRoot;
    private readonly FlowDocumentScrollViewer _content;
    private readonly bool _showDocxExport;
    private string _currentSourcePath;
    private string _markdownContent;

    public ReadmeWindow(
        string readme,
        string assetsRoot,
        string sourcePath,
        string title = "Windows Audit — Read Me",
        bool showDocxExport = false)
    {
        _assetsRoot = Path.GetFullPath(assetsRoot);
        _currentSourcePath = Path.GetFullPath(sourcePath);
        _markdownContent = readme;
        _showDocxExport = showDocxExport;
        Title = title;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/WindowsAudit;component/Assets/WindowsAudit.ico"));
        Width = 860;
        Height = 720;
        MinWidth = 600;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(244, 247, 247));

        var layout = new DockPanel { Margin = new Thickness(18) };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        if (_showDocxExport)
        {
            var export = new Button
            {
                Content = "_Export DOCX",
                ToolTip = "Export DOCX (Alt+E)",
                Padding = new Thickness(18, 9, 18, 9),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 110
            };
            export.Click += ExportDocx_Click;
            actions.Children.Add(export);
        }

        var close = new Button
        {
            Content = "_OK",
            ToolTip = "Close (Alt+O, Enter, or Escape)",
            Padding = new Thickness(18, 9, 18, 9),
            MinWidth = 88,
            IsDefault = true,
            IsCancel = true
        };
        close.Click += (_, _) => Close();
        actions.Children.Add(close);
        DockPanel.SetDock(actions, Dock.Bottom);
        layout.Children.Add(actions);

        _content = new FlowDocumentScrollViewer
        {
            Document = RenderMarkdown(readme),
            IsSelectionEnabled = true,
            IsToolBarVisible = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(18),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(215, 225, 226))
        };
        layout.Children.Add(_content);
        Content = layout;
        Accessibility.Configure(this, layout, horizontalScrolling: false);
        Loaded += (_, _) => ScrollToDocumentTop();
    }

    private FlowDocument RenderMarkdown(string markdown)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(28, 48, 56)),
            PagePadding = new Thickness(2)
        };

        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        System.Windows.Documents.List? currentList = null;
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0) return;
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 12), LineHeight = 20 };
            AddInlineMarkdown(paragraph.Inlines, string.Join(" ", paragraphLines));
            document.Blocks.Add(paragraph);
            paragraphLines.Clear();
        }

        foreach (var line in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (line.TrimStart().StartsWith("<!--", StringComparison.Ordinal) &&
                line.TrimEnd().EndsWith("-->", StringComparison.Ordinal))
                continue;

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                currentList = null;
                if (inCodeBlock)
                {
                    var code = new Paragraph
                    {
                        Margin = new Thickness(0, 0, 0, 14),
                        Padding = new Thickness(12, 9, 12, 9),
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 245)),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 13
                    };
                    code.Inlines.Add(new Run(string.Join(Environment.NewLine, codeLines)));
                    document.Blocks.Add(code);
                    codeLines.Clear();
                }
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                currentList = null;
                continue;
            }

            if (HorizontalRulePattern.IsMatch(line))
            {
                FlushParagraph();
                currentList = null;
                document.Blocks.Add(new Paragraph
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(96, 116, 123)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Margin = new Thickness(0, 8, 0, 12),
                    FontSize = 1,
                    LineHeight = 1
                });
                continue;
            }

            var heading = HeadingPattern.Match(line);
            if (heading.Success)
            {
                FlushParagraph();
                currentList = null;
                var level = heading.Groups[1].Length;
                var headingParagraph = new Paragraph
                {
                    Margin = new Thickness(0, level == 1 ? 2 : 8, 0, level <= 2 ? 10 : 6),
                    FontSize = level switch { 1 => 25, 2 => 20, 3 => 17, _ => 15 },
                    FontWeight = FontWeights.SemiBold
                };
                AddInlineMarkdown(headingParagraph.Inlines, heading.Groups[2].Value);
                document.Blocks.Add(headingParagraph);
                continue;
            }

            var listItem = ListPattern.Match(line);
            if (listItem.Success)
            {
                FlushParagraph();
                var isOrdered = char.IsDigit(listItem.Groups["marker"].Value[0]);
                if (currentList is null || (currentList.MarkerStyle == TextMarkerStyle.Decimal) != isOrdered)
                {
                    currentList = new System.Windows.Documents.List
                    {
                        MarkerStyle = isOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                        Margin = new Thickness(0, 0, 0, 12),
                        Padding = new Thickness(18, 0, 0, 0)
                    };
                    document.Blocks.Add(currentList);
                }
                var itemParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 3), LineHeight = 20 };
                AddInlineMarkdown(itemParagraph.Inlines, listItem.Groups["text"].Value);
                currentList.ListItems.Add(new ListItem(itemParagraph));
                continue;
            }

            currentList = null;
            paragraphLines.Add(line);
        }

        if (inCodeBlock) codeLines.Add(string.Empty);
        if (codeLines.Count > 0)
        {
            var code = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(12, 9, 12, 9),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 245)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };
            code.Inlines.Add(new Run(string.Join(Environment.NewLine, codeLines)));
            document.Blocks.Add(code);
        }
        FlushParagraph();
        Accessibility.ApplyMode(document);
        return document;
    }

    private void AddInlineMarkdown(InlineCollection inlines, string text)
    {
        var position = 0;
        foreach (Match match in InlinePattern.Matches(text))
        {
            if (match.Index > position)
                inlines.Add(new Run(text[position..match.Index]));

            if (match.Groups["code"].Success)
            {
                inlines.Add(new Run(match.Value[1..^1])
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 245))
                });
            }
            else if (match.Groups["bold"].Success)
            {
                inlines.Add(new Bold(new Run(match.Value[2..^2])));
            }
            else if (match.Groups["italic"].Success)
            {
                inlines.Add(new Italic(new Run(match.Value[1..^1])));
            }
            else if (match.Groups["link"].Success)
            {
                var separator = match.Value.IndexOf("](", StringComparison.Ordinal);
                var label = match.Value[1..separator];
                var target = match.Value[(separator + 2)..^1];
                if (TryResolveLink(target, out var uri))
                {
                    var hyperlink = new Hyperlink(new Run(label));
                    hyperlink.NavigateUri = uri;
                    hyperlink.RequestNavigate += OpenLink;
                    inlines.Add(hyperlink);
                }
                else
                {
                    inlines.Add(new Run(label));
                }
            }

            position = match.Index + match.Length;
        }

        if (position < text.Length)
            inlines.Add(new Run(text[position..]));
    }

    private bool TryResolveLink(string target, out Uri uri)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out var externalUri) &&
            externalUri.Scheme is "https" or "http" or "mailto")
        {
            uri = externalUri;
            return true;
        }

        var targetPath = target.Split('#', 2)[0];
        if (!string.Equals(Path.GetExtension(targetPath), ".md", StringComparison.OrdinalIgnoreCase))
        {
            uri = null!;
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(_currentSourcePath)!,
            targetPath.Replace('/', Path.DirectorySeparatorChar)));
        var relativePath = Path.GetRelativePath(_assetsRoot, fullPath);
        if (relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) || !File.Exists(fullPath))
        {
            uri = null!;
            return false;
        }

        uri = new System.Uri(fullPath);
        return true;
    }

    private void OpenLink(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri.IsFile)
        {
            var targetPath = Path.GetFullPath(e.Uri.LocalPath);
            var relativePath = Path.GetRelativePath(_assetsRoot, targetPath);
            if (relativePath != ".." && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !Path.IsPathRooted(relativePath) &&
                string.Equals(Path.GetExtension(targetPath), ".md", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(targetPath))
            {
                _currentSourcePath = targetPath;
                _markdownContent = File.ReadAllText(targetPath);
                _content.Document = RenderMarkdown(_markdownContent);
                Title = $"Windows Audit — {Path.GetFileNameWithoutExtension(targetPath)}";
                ScrollToDocumentTop();
            }
            e.Handled = true;
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        e.Handled = true;
    }

    private void ExportDocx_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Results as Word document",
            InitialDirectory = AppContext.BaseDirectory,
            Filter = "Word documents (*.docx)|*.docx",
            DefaultExt = ".docx",
            AddExtension = true,
            FileName = $"{Path.GetFileNameWithoutExtension(_currentSourcePath)}.docx"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            DocxReportWriter.Write(dialog.FileName, _markdownContent);
            MessageBox.Show(this, $"Results exported to:\n{dialog.FileName}", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "DOCX export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScrollToDocumentTop() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            FindVisualChild<ScrollViewer>(_content)?.ScrollToTop()));

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T result) return result;

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null) return descendant;
        }

        return null;
    }
}