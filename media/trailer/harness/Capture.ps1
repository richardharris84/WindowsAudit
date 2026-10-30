param(
    [Parameter(Mandatory)]
    [ValidateSet('services', 'apps', 'startup', 'explorer', 'explorer-refresh')]
    [string]$Kind,
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class TrailerNative {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hwnd,int x,int y,int width,int height,bool repaint);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rectangle);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
}
'@
$root = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $root -Force | Out-Null
function Get-UtilityWindow([string]$kind) {
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($window in $windows) {
        $name = $window.Current.Name
        $class = $window.Current.ClassName
        if (($kind -eq 'Services' -and $name -eq 'Services' -and $class -eq 'MMCMainFrame') -or
            ($kind -eq 'Settings' -and $name -eq 'Settings') -or
            ($kind -eq 'Explorer' -and $class -eq 'CabinetWClass' -and
                $window.Current.NativeWindowHandle -notin $script:existingExplorer)) {
            return $window
        }
    }
    throw "Could not locate $kind window."
}
function Save-Capture([System.Windows.Automation.AutomationElement]$window, [string]$name) {
    $handle = [IntPtr]$window.Current.NativeWindowHandle
    [IO.File]::WriteAllText("$root\$name-handle.txt", $handle.ToInt64().ToString())
    [void][TrailerNative]::ShowWindow($handle, 9)
    [void][TrailerNative]::MoveWindow($handle, 40, 35, 1360, 850, $true)
    [void][TrailerNative]::SetForegroundWindow($handle)
    Start-Sleep -Seconds 3
    if ([TrailerNative]::GetForegroundWindow() -ne $handle) { throw 'Utility did not become foreground; refusing to capture unrelated content.' }
    $rect = [TrailerNative+Rect]::new()
    [void][TrailerNative]::GetWindowRect($handle, [ref]$rect)
    $bitmap = [Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($rect.Left,$rect.Top,0,0,$bitmap.Size)
        $bitmap.Save("$root\$name-raw.png", [Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
    $elements = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $safeNames = @('Services (Local)','Name','Description','Status','Startup Type','Log On As',
        'Search apps','Search this list','Installed apps','Apps & features','Startup apps','Startup',
        'Apps','Settings','Search for a setting','Find a setting','Sort by','Filter by','Search box',
        'Search Trailer Demo','Search Results in Trailer Demo')
    foreach ($element in $elements) {
        if ($element.Current.Name -in $safeNames -or $element.Current.ControlType.ProgrammaticName -in @('ControlType.List','ControlType.DataGrid','ControlType.Tree')) {
            $bounds = $element.Current.BoundingRectangle
            if ($bounds.Width -gt 0) {
                [pscustomobject]@{Type=$element.Current.ControlType.ProgrammaticName;Name=$element.Current.Name;
                    X=[int]($bounds.X-$rect.Left);Y=[int]($bounds.Y-$rect.Top);W=[int]$bounds.Width;H=[int]$bounds.Height}
            }
        }
    }
}
switch ($Kind) {
    'services' {
        Start-Process services.msc
        Start-Sleep -Seconds 4
        Save-Capture (Get-UtilityWindow 'Services') 'services'
    }
    'apps' {
        Start-Process 'ms-settings:appsfeatures'
        Start-Sleep -Seconds 5
        Save-Capture (Get-UtilityWindow 'Settings') 'apps'
    }
    'startup' {
        Start-Process 'ms-settings:startupapps'
        Start-Sleep -Seconds 5
        Save-Capture (Get-UtilityWindow 'Settings') 'startup'
    }
    'explorer' {
        $script:existingExplorer = @(
            [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
                [System.Windows.Automation.TreeScope]::Children,
                [System.Windows.Automation.Condition]::TrueCondition) |
            Where-Object { $_.Current.ClassName -eq 'CabinetWClass' } |
            ForEach-Object { $_.Current.NativeWindowHandle })
        Start-Process explorer.exe -ArgumentList '/n,C:\'
        Start-Sleep -Seconds 4
        $window = Get-UtilityWindow 'Explorer'
        $edits = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Edit))
        $search = @($edits | Where-Object { $_.Current.Name.Trim() -like 'Search*' }) | Select-Object -First 1
        if ($null -eq $search) { throw 'Could not find Explorer search box.' }
        [void][TrailerNative]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle)
        $search.SetFocus()
        $value = $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        $value.SetValue('size:>10MB')
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
        Start-Sleep -Seconds 10
        Save-Capture $window 'explorer'
    }
    'explorer-refresh' {
        $handle = [IntPtr][long][IO.File]::ReadAllText("$root\explorer-handle.txt")
        $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
        Save-Capture $window 'explorer'
    }
    default { throw 'Specify services, apps, startup or explorer.' }
}
