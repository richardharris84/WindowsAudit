using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Automation;
using System.Windows.Data;

namespace WindowsServiceAudit.App;

internal static class Accessibility
{
    private static readonly List<WeakReference<FrameworkElement>> Layouts = [];
    private static double _scale = 1;
    private static readonly List<WeakReference<Window>> Windows = [];
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, (object Value, bool Local, BindingBase? Binding)>> Originals = new();
    public static bool Enabled { get; private set; }

    private static void Remember(DependencyObject element, DependencyProperty property)
    {
        var values = Originals.GetOrCreateValue(element);
        values.TryAdd(property, (element.GetValue(property),
            element.ReadLocalValue(property) != DependencyProperty.UnsetValue,
            BindingOperations.GetBindingBase(element, property)));
    }

    private static void Set(DependencyObject element, DependencyProperty property, object value)
    {
        Remember(element, property);
        element.SetValue(property, value);
    }

    private static void Resource(FrameworkElement element, DependencyProperty property, object key)
    {
        Remember(element, property);
        element.SetResourceReference(property, key);
    }

    private static void Resource(FrameworkContentElement element, DependencyProperty property, object key)
    {
        Remember(element, property);
        element.SetResourceReference(property, key);
    }

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled) _scale = 1;
        Windows.RemoveAll(reference => !reference.TryGetTarget(out _));
        foreach (var reference in Windows)
            if (reference.TryGetTarget(out var window))
                ApplyMode(window);
        ChangeScale(0);
    }

    public static void ApplyMode(DependencyObject element)
    {
        if (Originals.TryGetValue(element, out var values))
            foreach (var (property, original) in values)
                if (original.Binding is not null) BindingOperations.SetBinding(element, property, original.Binding);
                else if (original.Local) element.SetValue(property, original.Value);
                else element.ClearValue(property);
        if (Enabled)
        {
            if (element is Window window)
            {
                Resource(window, Window.BackgroundProperty, SystemColors.WindowBrushKey);
                Resource(window, Window.ForegroundProperty, SystemColors.WindowTextBrushKey);
            }
            ApplySystemColors(element);
        }
        else
        {
            if (element is Button button && button.Content is string content)
                Set(button, ContentControl.ContentProperty, content.Replace("_", ""));
            if (element is Label label && label.Content is string caption)
            {
                Set(label, ContentControl.ContentProperty, caption.Replace("_", ""));
                Set(label, Label.TargetProperty, null!);
            }
            foreach (var property in new[] { AutomationProperties.NameProperty, AutomationProperties.LabeledByProperty,
                AutomationProperties.HelpTextProperty })
                if (element.ReadLocalValue(property) != DependencyProperty.UnsetValue)
                {
                    Remember(element, property);
                    element.ClearValue(property);
                }
            if (element is UIElement)
                Set(element, AutomationProperties.LiveSettingProperty, AutomationLiveSetting.Off);
            if (element is FrameworkElement control && control.ToolTip is string tip)
            {
                var index = tip.IndexOf(" (Alt+", StringComparison.Ordinal);
                if (index >= 0) Set(control, FrameworkElement.ToolTipProperty, tip[..index]);
            }
        }
        foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>())
            ApplyMode(child);
    }

    public static void Configure(Window window, FrameworkElement layout, double minimumWidth = 400, bool horizontalScrolling = true)
    {
        Windows.Add(new WeakReference<Window>(window));
        layout.LayoutTransform = new ScaleTransform(_scale, _scale);
        layout.MinWidth = horizontalScrolling ? minimumWidth : 0;
        Layouts.Add(new WeakReference<FrameworkElement>(layout));
        if (horizontalScrolling && window.Content == layout)
        {
            window.Content = null;
            window.Content = new ScrollViewer
            {
                Content = layout,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Focusable = false
            };
        }
        if (layout.Parent is ScrollViewer scroll)
        {
            scroll.SizeChanged += (_, _) => UpdateWidth(layout);
            UpdateWidth(layout);
        }
        ApplyMode(window);
        window.PreviewKeyDown += (_, e) =>
        {
            if (!Enabled) return;
            if (Keyboard.Modifiers != ModifierKeys.Control &&
                !(Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.OemPlus))
                return;
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    ChangeScale(0.25);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ChangeScale(-0.25);
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ChangeScale(null);
                    break;
                default:
                    return;
            }
            e.Handled = true;
        };
    }

    private static void UpdateWidth(FrameworkElement layout)
    {
        if (layout.Parent is ScrollViewer scroll)
            layout.Width = Math.Max(layout.MinWidth, scroll.ActualWidth / _scale);
    }

    public static void ApplySystemColors(DependencyObject element)
    {
        if (element is Control control)
        {
            Resource(control, Control.BackgroundProperty, SystemColors.WindowBrushKey);
            Resource(control, Control.ForegroundProperty, SystemColors.WindowTextBrushKey);
            Resource(control, Control.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);
            Resource(control, Control.FocusVisualStyleProperty, "AccessibleFocus");
        }
        if (element is Panel panel)
            Resource(panel, Panel.BackgroundProperty, SystemColors.WindowBrushKey);
        if (element is Border border)
        {
            Resource(border, Border.BackgroundProperty, SystemColors.WindowBrushKey);
            Resource(border, Border.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);
        }
        if (element is TextBlock text)
            Resource(text, TextBlock.ForegroundProperty, SystemColors.WindowTextBrushKey);
        if (element is FlowDocument document)
            Resource(document, FlowDocument.ForegroundProperty, SystemColors.WindowTextBrushKey);
        if (element is TextElement documentElement)
        {
            if (documentElement is Hyperlink ||
                documentElement.ReadLocalValue(TextElement.ForegroundProperty) != DependencyProperty.UnsetValue)
                Resource(documentElement, TextElement.ForegroundProperty,
                    documentElement is Hyperlink ? SystemColors.HotTrackBrushKey : SystemColors.WindowTextBrushKey);
            if (documentElement.ReadLocalValue(TextElement.BackgroundProperty) != DependencyProperty.UnsetValue)
                Resource(documentElement, TextElement.BackgroundProperty, SystemColors.WindowBrushKey);
            if (documentElement is Block block)
                Resource(block, Block.BorderBrushProperty, SystemColors.ActiveBorderBrushKey);
        }
    }

    public static void ChangeScale(double? adjustment)
    {
        _scale = !Enabled || adjustment is null ? 1 : Math.Clamp(_scale + adjustment.Value, 1, 2);
        Layouts.RemoveAll(reference => !reference.TryGetTarget(out _));
        foreach (var reference in Layouts)
            if (reference.TryGetTarget(out var layout))
            {
                layout.LayoutTransform = new ScaleTransform(_scale, _scale);
                UpdateWidth(layout);
            }
    }
}
