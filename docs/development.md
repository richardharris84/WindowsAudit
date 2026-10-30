# Developer Guide

[Recreate the project with Copilot prompts](../Copilot-Prompts.md) covers the PowerShell prototype, WPF application, packaging, Store assets, and testing, with recovered requests distinguished from reconstructed prompts.

## Download Source Code

[https://github.com/richardharris84/](https://github.com/richardharris84/) `<TBD>`

## Run from Source

Requirements: Windows 10 or Windows 11 x64 and the .NET 10 SDK.

From the repository root:

```powershell
dotnet run --project .\src\WindowsServiceAudit.App\WindowsServiceAudit.App.csproj
```

## Technology

### Windows Presentation Foundation (WPF)

This project uses WPF, a mature framework for managed Windows desktop applications on modern .NET and the full .NET Framework. WPF uses XAML markup alongside C# code to build the interface, and provides data binding, customizable styles and templates, and rich graphics capabilities.

The app targets `net10.0-windows`; WPF provides its main window, menus, audit grid, and Markdown dialogs. WPF and UWP share concepts such as XAML and data binding, so those skills transfer, but they are separate frameworks with different APIs and application models.

## Repository Layout

```text
WindowsAudit/
|-- .gitignore                             # Excludes generated outputs and local/private data
|-- README.md                              # End-user introduction and safety notes
|-- Copilot-Prompts.md                      # Ordered reconstruction prompts and recovered history
|-- WindowsServiceAudit.slnx                # .NET solution
|-- artifacts/                             # Generated outputs and local build tools
|   |-- branding/                          # Store logo PNGs (1024, 300, 150, and 71 px)
|   |-- installer/                         # WindowsAudit-Setup.exe
|   |-- msix/                              # Unsigned Store-submission MSIX
|   |-- msix-test/                         # Signed local-test MSIX, public CER, TESTING.txt
|   |-- publish/
|   |   `-- win-x64/                       # Self-contained executable and runtime assets
|   |-- tools/                             # Locally obtained SDK packaging tools
|   `-- trailer/
|       |-- WindowsAudit-Trailer-60s-Store.mp4
|       `-- WindowsAudit-Trailer-60s-Thumbnail.png
|-- docs/
|   |-- development.md                      # Build, publish, architecture, and project layout
|   `-- faq.md                              # Common questions and privacy details
|-- installer/
|   |-- Build-Installer.ps1                 # Self-contained publish and Inno Setup build
|   `-- WindowsAudit.iss                    # Per-user installer and shortcut definitions
|-- packaging/
|   |-- AppxManifest.xml                    # Store identity, capabilities, and package assets
|   `-- Build-MSIX.ps1                      # Publish, generate logos, and package with MakeAppx
|-- src/
|   |-- WindowsServiceAudit.App/
|   |   |-- App.xaml                         # WPF application resources
|   |   |-- App.xaml.cs                      # Application startup
|   |   |-- AssemblyInfo.cs                  # WPF theme resource lookup metadata
|   |   |-- MainWindow.xaml                  # Main window layout and menus
|   |   |-- MainWindow.xaml.cs               # Audit and tool commands
|   |   |-- ReadmeWindow.cs                  # Markdown help and results dialog
|   |   |-- WhitelistEditorWindow.cs         # In-app whitelist editor
|   |   |-- AuditFolderSettings.cs           # Persisted output-folder preference
|   |   |-- Assets/
|   |   |   |-- Broom.xaml                   # Transparent broom and sparkles artwork
|   |   |   |-- WindowsAudit.ico             # Multi-resolution executable and window icon
|   |   |   `-- whitelist.csv                # Starter whitelist
|   |   `-- WindowsServiceAudit.App.csproj   # WPF app project and packaged content
|   `-- WindowsServiceAudit.Core/
|       |-- AuditRunner.cs                   # Local Windows service inventory
|       |-- AuditDataDirectory.cs            # Writable audit-folder selection
|       |-- ServiceAuditRecord.cs            # Inventory record model
|       |-- ServicePathParser.cs             # Service executable path parsing
|       |-- Whitelist.cs                     # Whitelist CSV loading and matching
|       |-- CsvReportWriter.cs              # CSV report generation
|       |-- DocxReportWriter.cs             # Word document report generation
|       |-- ResultsWriter.cs                # Dynamic Win-Srv-Eval-Report.md generation
|       `-- WindowsServiceAudit.Core.csproj  # Core library project
`-- tests/
    `-- WindowsServiceAudit.Tests/
        |-- AuditCoreTests.cs                 # Core and report regression tests
        |-- MSTestSettings.cs                 # Test-runner settings
        `-- WindowsServiceAudit.Tests.csproj  # MSTest project
```

Build-generated `bin/`, `obj/`, and `TestResults/` directories and the expanded
contents of published assets and downloaded tools are omitted. `artifacts/`
contains generated or locally obtained files, not maintained application source;
its package versions and contents depend on which build steps have run.
The signed test package and public certificate are for local testing only.

`.gitignore` excludes `artifacts/`, build/test outputs, generated service reports,
local settings and whitelist files, and signing materials. The starter whitelist
under `src/WindowsServiceAudit.App/Assets/` remains versioned. Share installers
and the trailer as GitHub Release attachments rather than committing binaries;
review any exported reports for identifying information before sharing.

The root README, `Copilot-Prompts.md`, and `docs/` Markdown files are copied into
the app's output `Assets/` directory during builds. Existing installer, MSIX, and
publish outputs are not automatically refreshed when source documentation changes.

## Build and Test

Build the solution:

```powershell
dotnet build .\WindowsServiceAudit.slnx
```

Run the automated tests:

```powershell
dotnet test .\WindowsServiceAudit.slnx
```

The tests cover whitelist loading and matching, service path parsing and target fallback, CSV schema and escaping, DOCX report generation and validation, and generated-section behavior in `Win-Srv-Eval-Report.md`.

## Self-Contained Publish

Publish a self-contained Windows x64 build so users do not need to install .NET:

```powershell
dotnet publish .\src\WindowsServiceAudit.App\WindowsServiceAudit.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Build the Setup Program

Install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then run:

```powershell
.\installer\Build-Installer.ps1
```

If the compiler is installed elsewhere, pass `-IsccPath 'C:\path\to\ISCC.exe'`.
The script publishes a self-contained Windows x64 application and creates
`artifacts\installer\WindowsAudit-Setup.exe`. Distribute this one installer; users
do not need to unzip files or install .NET.

Setup installs for the current user under `%LOCALAPPDATA%\Programs\Windows Audit`
without administrator rights, adds a Start menu shortcut, offers an optional desktop
shortcut, and registers an uninstaller. Existing user-created audit files and
settings are not explicitly deleted by the uninstaller.

The installer is unsigned. For distribution, sign the executable and installer with a trusted Authenticode code-signing certificate, timestamp the signature, protect the private key, and publish a SHA-256 checksum. Unsigned or untrusted binaries may trigger SmartScreen or email-provider warnings; share a release link rather than attaching executables to email.

## Microsoft Store MSIX

The Store package uses the reserved identity in `packaging/AppxManifest.xml`:
`BuiltbyRichardHarris.WindowsAudit`, publisher
`CN=2CDAA861-DB03-4AFA-B278-C278B732D5D4`, and display publisher
`Built by Richard Harris`.

With Windows SDK MakeAppx available, run:

```powershell
pwsh -STA -File .\packaging\Build-MSIX.ps1
```

Use `-MakeAppxPath` if the SDK tool cannot be found automatically. The script also
detects Microsoft SDK Build Tools extracted under `artifacts\tools\sdk-buildtools`.
It creates a self-contained, unsigned `artifacts\msix\WindowsAudit_1.0.0.0_x64.msix`
for the MSIX app submission in Partner Center. Microsoft signs the accepted Store
package; this does not sign the separate EXE installer. Increase `-Version` for
later submissions, keeping the fourth component zero.

The package declares `runFullTrust` because this is a WPF desktop application
that reads WMI, registry, and process information and opens Windows utilities.
Explain this capability during certification. MakeAppx validation is not Store
certification: packaged installation, scanning, exports, whitelist editing, and
Windows utility commands still need testing on Windows before submission.
Unsigned MSIX files cannot normally be installed by double-clicking; local testing
requires an appropriate development registration or a locally trusted test
signature. Do not ask public users to trust a development certificate.

MSIX installs into a protected package folder, so automatic report files and the
editable whitelist use the existing writable-folder fallback. Export dialogs
still allow users to choose a writable destination.

## Runtime Assets

`AuditFolderSettings` persists an optional selected directory in `%LOCALAPPDATA%\WindowsServiceAudit\settings.json`. `AuditDataDirectory.Resolve` validates that configured directory and uses it without silently falling back if it becomes unavailable. When no directory is configured, it probes the application directory and uses it when writable; otherwise, `MainWindow` supplies `%LOCALAPPDATA%\WindowsServiceAudit` as the fallback. Audit reports and `whitelist.csv` share the resolved location; the editor saves with the existing `Whitelist.Save` format, and each audit reloads it through `Whitelist.LoadOrSeed`.

The app packages the root README, `Copilot-Prompts.md`, and files under `docs/` as local Markdown help. The app uses WMI, the Windows registry, process metadata, and file-signing metadata locally. It does not invoke PowerShell, an online AI service, or network APIs.

[← Back to the README](../README.md)

---

Developed by: [Richard Harris](https://github.com/richardharris84)