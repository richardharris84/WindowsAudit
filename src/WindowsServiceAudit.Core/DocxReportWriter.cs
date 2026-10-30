using System.IO.Packaging;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsServiceAudit.Core;

public static partial class DocxReportWriter
{
    [GeneratedRegex(@"^(#{1,6})\s+(.+?)\s*#*\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"^\s*(?<marker>[-*+]|\d+\.)\s+(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ListPattern();

    [GeneratedRegex(@"(?<code>`[^`]+`)|(?<bold>\*\*.+?\*\*)|(?<italic>(?<!\\)\*[^*]+(?<!\\)\*)|(?<link>\[[^\]]+\]\([^)]+\))", RegexOptions.CultureInvariant)]
    private static partial Regex InlinePattern();

    [GeneratedRegex(@"\\([\\`*_{}\[\]()#+.!|>-])", RegexOptions.CultureInvariant)]
    private static partial Regex EscapedMarkdownPattern();

    public static void Write(string path, string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(markdown);

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using (var document = WordprocessingDocument.Create(fullPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            AppendMarkdown(body, mainPart, markdown);

            body.Append(new SectionProperties(
                new PageSize { Width = 12240U, Height = 15840U },
                new PageMargin { Top = 1440, Right = 1440U, Bottom = 1440, Left = 1440U }));
            mainPart.Document.Save();
        }

        // OpenOffice rejects the SDK's absolute package-root document target.
        using var package = Package.Open(fullPath, FileMode.Open, FileAccess.ReadWrite);
        var relationship = package.GetRelationshipsByType(
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").Single();
        var relativeTarget = new Uri(relationship.TargetUri.OriginalString.TrimStart('/'), UriKind.Relative);
        package.DeleteRelationship(relationship.Id);
        package.CreateRelationship(relativeTarget, TargetMode.Internal, relationship.RelationshipType, relationship.Id);
    }

    private static void AppendMarkdown(Body body, MainDocumentPart mainPart, string markdown)
    {
        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0) return;

            var paragraph = CreateParagraph();
            AppendInlineMarkdown(paragraph, mainPart, string.Join(" ", paragraphLines));
            body.Append(paragraph);
            paragraphLines.Clear();
        }

        void FlushCodeBlock()
        {
            var paragraph = CreateParagraph(after: 200, shaded: true);
            for (var index = 0; index < codeLines.Count; index++)
            {
                if (index > 0) paragraph.Append(new Run(new Break()));
                paragraph.Append(CreateRun(codeLines[index], code: true));
            }
            body.Append(paragraph);
            codeLines.Clear();
        }

        foreach (var line in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (line.TrimStart().StartsWith("<!--", StringComparison.Ordinal) &&
                line.TrimEnd().EndsWith("-->", StringComparison.Ordinal))
                continue;

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                if (inCodeBlock) FlushCodeBlock();
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
                continue;
            }

            var heading = HeadingPattern().Match(line);
            if (heading.Success)
            {
                FlushParagraph();
                var level = heading.Groups[1].Length;
                var paragraph = CreateParagraph(after: level <= 2 ? 200 : 120, keepWithNext: true);
                AppendInlineMarkdown(paragraph, mainPart, heading.Groups[2].Value, fontSize: level switch
                {
                    1 => 32,
                    2 => 26,
                    3 => 22,
                    _ => 20
                }, bold: true);
                body.Append(paragraph);
                continue;
            }

            var listItem = ListPattern().Match(line);
            if (listItem.Success)
            {
                FlushParagraph();
                var marker = char.IsDigit(listItem.Groups["marker"].Value[0]) ? "1. " : "\u2022 ";
                var paragraph = CreateParagraph(leftIndent: 360);
                paragraph.Append(CreateRun(marker));
                AppendInlineMarkdown(paragraph, mainPart, listItem.Groups["text"].Value);
                body.Append(paragraph);
                continue;
            }

            paragraphLines.Add(line);
        }

        if (inCodeBlock) FlushCodeBlock();
        FlushParagraph();
    }

    private static Paragraph CreateParagraph(
        int after = 160,
        int leftIndent = 0,
        bool keepWithNext = false,
        bool shaded = false)
    {
        var properties = new ParagraphProperties();
        if (keepWithNext) properties.Append(new KeepNext());
        if (shaded) properties.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = "F1F5F5" });
        properties.Append(new SpacingBetweenLines { After = after.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        if (leftIndent > 0)
            properties.Append(new Indentation { Left = leftIndent.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        return new Paragraph(properties);
    }

    private static void AppendInlineMarkdown(
        Paragraph paragraph,
        MainDocumentPart mainPart,
        string text,
        int fontSize = 22,
        bool bold = false)
    {
        var position = 0;
        foreach (Match match in InlinePattern().Matches(text))
        {
            if (match.Index > position)
                paragraph.Append(CreateRun(Unescape(text[position..match.Index]), fontSize, bold));

            if (match.Groups["code"].Success)
            {
                paragraph.Append(CreateRun(Unescape(match.Value[1..^1]), fontSize, bold, code: true));
            }
            else if (match.Groups["bold"].Success)
            {
                paragraph.Append(CreateRun(Unescape(match.Value[2..^2]), fontSize, bold: true));
            }
            else if (match.Groups["italic"].Success)
            {
                paragraph.Append(CreateRun(Unescape(match.Value[1..^1]), fontSize, bold, italic: true));
            }
            else if (match.Groups["link"].Success)
            {
                var separator = match.Value.IndexOf("](", StringComparison.Ordinal);
                var label = Unescape(match.Value[1..separator]);
                var target = Unescape(match.Value[(separator + 2)..^1]);
                if (Uri.TryCreate(target, UriKind.Absolute, out var uri) &&
                    uri.Scheme is "http" or "https" or "mailto")
                {
                    var relationship = mainPart.AddHyperlinkRelationship(uri, isExternal: true);
                    var hyperlink = new Hyperlink { Id = relationship.Id };
                    hyperlink.Append(CreateRun(label, fontSize, bold, hyperlink: true));
                    paragraph.Append(hyperlink);
                }
                else
                {
                    paragraph.Append(CreateRun(label, fontSize, bold));
                }
            }

            position = match.Index + match.Length;
        }

        if (position < text.Length)
            paragraph.Append(CreateRun(Unescape(text[position..]), fontSize, bold));
    }

    private static Run CreateRun(
        string text,
        int fontSize = 22,
        bool bold = false,
        bool italic = false,
        bool code = false,
        bool hyperlink = false)
    {
        var properties = new RunProperties();
        properties.Append(new RunFonts
        {
            Ascii = code ? "Consolas" : "Segoe UI",
            HighAnsi = code ? "Consolas" : "Segoe UI"
        });
        if (bold) properties.Append(new Bold());
        if (italic) properties.Append(new Italic());
        if (hyperlink)
            properties.Append(new Color { Val = "0563C1" });
        properties.Append(new FontSize { Val = fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        if (hyperlink) properties.Append(new Underline { Val = UnderlineValues.Single });

        return new Run(properties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static string Unescape(string text) =>
        EscapedMarkdownPattern().Replace(text, match => match.Groups[1].Value);
}
