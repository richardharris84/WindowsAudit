using System.Globalization;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WindowsServiceAudit.App;
using WindowsServiceAudit.Core;

internal static partial class Program
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(28, 48, 56));
    private static readonly Brush Teal = new SolidColorBrush(Color.FromRgb(8, 126, 131));
    private static string Root = "";

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Supply an output folder containing a copy of the archived trailer frames.");
        Root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Root);
        var app = new App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (args.Length > 1 && args[1] == "scan")
        {
            ScanShowcase(app);
            return;
        }
        var original = new MainWindow();
        var content = (DockPanel)original.Content;
        original.Content = null;
        var host = new Window
        {
            Content = content, Width = 1600, Height = 800, Left = 25, Top = 30,
            ShowInTaskbar = false, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize
        };
        host.Show();
        host.UpdateLayout();
        var menu = (Menu)content.Children[0];
        var tools = menu.Items.OfType<MenuItem>().Single(item => item.Header.ToString() == "Tools");
        var entries = tools.Items.OfType<MenuItem>().ToArray();
        var expected = new[] { "Scan Windows Services…", "Stop / Disable Services", "Add or remove programs", "Startup Apps", "Cleanup Large Files" };
        if (!entries.Select(item => item.Header.ToString()).SequenceEqual(expected))
            throw new InvalidOperationException("Tools menu changed; update trailer coverage.");
        for (var index = 0; index < entries.Length; index++)
        {
            if (index == 1)
            {
                var records = (ObservableCollection<ServiceAuditRecord>)((DataGrid)original.FindName("ServicesGrid")).ItemsSource;
                var names = new[] { "Example Cloud Sync","Example Software Updater","Demo Audio Service","Demo Backup Agent",
                    "Demo Bluetooth Support","Demo Device Manager","Demo Event Service","Demo Network Helper","Demo Print Service",
                    "Demo Search Indexer","Demo Security Service","Demo Time Service" };
                var ids = new[] { "ExampleSync","ExampleUpdater","DemoAudio","DemoBackup","DemoBluetooth","DemoDevice","DemoEvent",
                    "DemoNetwork","DemoPrint","DemoSearch","DemoSecurity","DemoTime" };
                var memory = new[] { 42.6,12.4,18.5,84.2,9.3,7.8,16.9,23.1,11.6,65.7,106.3,4.2 };
                for (var row=0; row<names.Length; row++)
                    records.Add(new ServiceAuditRecord
                    {
                        Name=ids[row],DisplayName=names[row],Status=row<2?"REVIEW":"OK",WorkingSetMB=memory[row],
                        State="Running",StartMode="Auto",StartName="LocalSystem",
                        ExecutablePath=$@"C:\Demo\{ids[row]}.exe",PathStatus="Fictional demo data"
                    });
                ((TextBlock)original.FindName("TotalCountText")).Text="12";
                ((TextBlock)original.FindName("ReviewCountText")).Text="2";
                ((TextBlock)original.FindName("StatusText")).Text="Demo scan complete. Fictional service data.";
                host.UpdateLayout();
            }
            tools.IsSubmenuOpen = true;
            entries[index].Focus();
            Pump();
            tools.ApplyTemplate();
            var popup = (Popup)tools.Template.FindName("PART_Popup", tools);
            if (popup?.Child is not FrameworkElement panel || panel.ActualWidth <= 0)
                throw new InvalidOperationException("Actual Tools menu popup did not render.");
            Save($"menu-{index}", drawing =>
            {
                Text(drawing, $"Tools > {expected[index].TrimEnd('…')}", 72, 35, 38, Ink, true);
                drawing.DrawRectangle(new VisualBrush(content), null, new Rect(160,115,1600,800));
                var location = tools.TranslatePoint(new Point(0,tools.ActualHeight), content);
                drawing.DrawRectangle(new VisualBrush(panel), new Pen(Teal,1),
                    new Rect(160+location.X,115+location.Y,panel.ActualWidth,panel.ActualHeight));
                Text(drawing, "Built-in shortcuts to review your PC.", 72, 950, 27, Ink);
                Footer(drawing, "DEMONSTRATION • NO SETTINGS CHANGED");
            });
        }
        tools.IsSubmenuOpen = false;
        ((ObservableCollection<ServiceAuditRecord>)((DataGrid)original.FindName("ServicesGrid")).ItemsSource).Clear();
        ((TextBlock)original.FindName("TotalCountText")).Text="Scanning";
        ((TextBlock)original.FindName("ReviewCountText")).Text="—";
        ((TextBlock)original.FindName("StatusText")).Text = "Demonstration scan in progress...";
        ((ProgressBar)original.FindName("AuditProgressBar")).Visibility = Visibility.Visible;
        host.UpdateLayout();
        Save("scan", drawing =>
        {
            Text(drawing,"Scan Windows Services",72,35,38,Ink,true);
            drawing.DrawRectangle(new VisualBrush(content),null,new Rect(160,115,1600,800));
            Text(drawing,"Gather service details locally. Review the results before making changes.",72,950,27,Ink);
            Footer(drawing,"SIMULATED SCAN • FICTIONAL INVENTORY IN NEXT SCENE");
        });
        host.Close();
        original.Close();
        Utility("services", "Stop / Disable Services", "Open Windows Services. Review a service before changing it.",
            "Service inventory concealed", true);
        Utility("apps", "Add or remove programs", "Open Installed apps to review software installed on your PC.",
            "Installed app identities concealed", true);
        Utility("startup", "Startup Apps", "Open Startup settings to review apps that launch when you sign in.",
            "App identities concealed • No toggles changed", true);
        Utility("explorer", "Cleanup Large Files", "Find files larger than 10 MB in File Explorer. Review before deleting.",
            "Actual C: drive search • No files deleted", false);
        app.Shutdown();
        Console.WriteLine("Rendered all five Tools menu choices; utility scenes use reviewed captures or archived frames.");
    }

    private static void Utility(string name, string title, string caption, string notice, bool privacy)
    {
        if (!File.Exists(Path.Combine(Root, name + ".png")))
        {
            if (File.Exists(Path.Combine(Root, "utility-" + name + ".png"))) return;
            throw new FileNotFoundException($"Provide a reviewed capture or archived utility-{name}.png frame.");
        }
        var image = Load(name + ".png");
        Save("utility-"+name, drawing =>
        {
            Text(drawing, title,72,35,38,Ink,true);
            if (name == "explorer")
            {
                var header = new CroppedBitmap(image,new Int32Rect(0,44,image.PixelWidth,116));
                drawing.DrawImage(header,new Rect(160,180,1600,116*1600.0/image.PixelWidth));
                drawing.DrawRoundedRectangle(null,new Pen(Teal,3),new Rect(1358,183,380,46),6,6);
                Text(drawing,"Search C:\\ for files larger than 10 MB",210,450,43,Ink,true);
                Text(drawing,"size:>10MB",210,535,64,Teal,true);
                Text(drawing,"Review file locations and sizes before deciding what to keep.",210,665,30,Ink);
                Text(drawing,"Windows Audit opens the search. It does not delete files.",210,725,30,Ink);
                Text(drawing,notice,210,825,24,Teal,true);
                Text(drawing,caption,72,950,27,Ink);
                Footer(drawing,"ACTUAL FILE EXPLORER • PRIVATE FILENAMES AND PATHS OMITTED • NO FILES DELETED");
                return;
            }
            var scale = Math.Min(1600.0/image.PixelWidth,800.0/image.PixelHeight);
            var width = image.PixelWidth*scale;
            var height = image.PixelHeight*scale;
            drawing.DrawImage(image,new Rect((1920-width)/2,115+(800-height)/2,width,height));
            var y = privacy ? 575 : 845;
            var text = Format(notice,22,Teal,true);
            drawing.DrawRoundedRectangle(Brushes.White,new Pen(Teal,1),
                new Rect(960-text.Width/2-24,y-12,text.Width+48,56),8,8);
            drawing.DrawText(text,new Point(960-text.Width/2,y));
            Text(drawing,caption,72,950,27,Ink);
            Footer(drawing,"ACTUAL WINDOWS UTILITY • IDENTIFYING DETAILS CONCEALED • NO CHANGES MADE");
        });
    }

    private static BitmapImage Load(string name)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(Path.Combine(Root,name));
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static void Pump() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { },DispatcherPriority.ApplicationIdle);

    private static FormattedText Format(string text,double size,Brush brush,bool bold=false) =>
        new(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,
                FontStretches.Normal),size,brush,1);

    private static void Text(DrawingContext drawing,string text,double x,double y,double size,Brush brush,bool bold=false) =>
        drawing.DrawText(Format(text,size,brush,bold),new Point(x,y));

    private static void Footer(DrawingContext drawing,string text) => Text(drawing,text,72,1030,18,Teal,true);

    private static void Save(string name,Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(242,246,246)),null,new Rect(0,0,1920,1080));
            draw(context);
        }
        var target = new RenderTargetBitmap(1920,1080,96,96,PixelFormats.Pbgra32);
        target.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        using var output = File.Create(Path.Combine(Root,name+".png"));
        encoder.Save(output);
    }
}
