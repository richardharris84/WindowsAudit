# Developer Guide

[Recreate the project with Copilot prompts](../Copilot-Prompts.md) covers the PowerShell prototype, WPF application, packaging, Store assets, and testing, with an explanation of the guide's sources and scope.

[GitHub Actions guide](../GitHubActions.md) describes CI, code scanning, script
analysis, and the Release workflow.

## Download Source Code

[Windows Audit on GitHub](https://github.com/richardharris84/WindowsAudit)

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
|-- .github/
|   |-- prompts/
|   |   `-- release-build.prompt.md         # Reusable full build and GitHub Release prompt
|   `-- workflows/                         # CI, scanning, automation, and Release action
|-- README.md                              # End-user introduction and safety notes
|-- Copilot-Prompts.md                      # Ordered reconstruction prompts, sources, and scope
|-- GitHubActions.md                        # GitHub Actions workflow guide
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
|-- media/
|   |-- Build-Branding.ps1                  # Render Store logos from the versioned broom drawing
|   `-- trailer/
|       |-- Build-Trailer.ps1               # Re-encode and validate the archived 60-second trailer
|       |-- thumbnail.png                  # Exact final thumbnail source
|       |-- frames/                        # 27 reviewed scene PNGs and timeline.txt
|       `-- harness/                       # Portable WPF demo source and optional capture helpers
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
|-- tests/
|   `-- WindowsServiceAudit.Tests/
|       |-- AuditCoreTests.cs                 # Core and report regression tests
|       |-- MSTestSettings.cs                 # Test-runner settings
|       `-- WindowsServiceAudit.Tests.csproj  # MSTest project
`-- tools/
    `-- Backup-Whitelist.ps1                # Verified private backup outside the workspace
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

## Accessibility Testing

**View > Enhanced accessibility** is off by default and is remembered in
`%LOCALAPPDATA%\WindowsServiceAudit\accessibility.json`, separately from the audit
folder setting. Invalid or inaccessible settings produce an explicit warning.
Menu access keys remain available regardless of this preference. Added button
and input access keys/automation labels, contrast styling, strong focus outlines,
zoom, and scan announcements are enabled only with the preference.
Disabling it restores standard styling and resets zoom to 100%.

With the preference enabled, the app uses dynamic Windows system colors and strong keyboard-focus outlines.
Interface zoom is available through the View menu and Ctrl+Plus, Ctrl+Minus, and
Ctrl+0 in the main window and dialogs. Zoom ranges from 100% to 200% for the
current session. Scan start, completion counts, and errors raise polite
UI Automation live-region events; progress updates are deliberately not announced.
See [Keyboard Navigation](../README.md#keyboard-navigation) for access keys.

Before releasing accessibility changes, manually verify:

- Start with no saved preference: confirm the standard appearance and working
  menu access keys. Enable/disable the option repeatedly, then restart and verify
  the choice is remembered and both existing/new dialogs follow the selected mode.
- With Narrator (Ctrl+Windows+Enter) and NVDA, navigate menus, buttons, filter,
  sample interval, inventory cells, whitelist editor, and report links. Confirm
  meaningful names, reading order, and scan completion/error announcements.
- Use only the keyboard: check Alt access keys, Tab/Shift+Tab order, arrow keys,
  visible focus, Escape, and access to Save/Cancel from the multiline editor.
- Enable a Windows contrast theme and switch it while the app is open. Check
  menus, selected grid cells, dialog text, links, and focus indicators.
- At Windows display scaling of 200% and app zoom of 200%, resize each window
  down to its minimum size. Check reflow, scrolling, and that all actions remain
  reachable without text overlap.
- Invite visually impaired users to complete a scan, filter results, edit the
  whitelist, and export a report; collect feedback before claiming usability.

Automated UI regression tests cover access-key uniqueness, labels, focus-style
and live-region metadata, zoom bounds, enlarged horizontal scrolling, and grid
sorting. They do not replace screen-reader, OS display-scaling, or user testing.

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
Add/Remove Programs displays `Windows Audit 1.0.2.0` with publisher
`Built by Richard Harris`; the installer derives its displayed version from
the published executable. The MSIX package display name also includes its version.

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
It creates a self-contained, unsigned `artifacts\msix\WindowsAudit_1.0.2.0_x64.msix`
for the MSIX app submission in Partner Center. Microsoft signs the accepted Store
package; this does not sign the separate EXE installer. By default the script
reads the application version from its project file. For later releases, update
the app project and package manifest together, keeping the fourth component zero.
An explicit `-Version` overrides the version for a standalone MSIX build only.

The GitHub **Release** action tests and builds both packages, then generates
`artifacts\SHA256SUMS.txt`. Push a matching tag (currently `v1.0.2.0`) to create
a draft GitHub Release with the installer, unsigned MSIX, and checksums attached.
A manual run on a branch uploads build artifacts without creating a release.
The action rejects mismatched app/manifest versions or a mismatched release tag.

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

## Create a Release with Copilot

The reusable [release-build prompt](../.github/prompts/release-build.prompt.md)
guides an agent through version updates, full EXE installer and MSIX builds,
tests, and a verified GitHub Release using the existing
[Release action](../.github/workflows/release.yml).

Open the repository root in VS Code and run the prompt in **Copilot Chat**, not
the terminal:

```text
/release-build
```

You can supply a version and publication preference:

```text
/release-build version 1.0.3.0, create a draft release
```

The version above is an example; choose an unused version newer than existing
releases. Without a version, the prompt chooses the next appropriate unused patch
version. EXE and MSIX versions remain identical, with four numeric components
and a final `.0` for Store compatibility. Installed display names include the
version and retain publisher `Built by Richard Harris`.

The prompt defaults to a **draft** release. Add `publish publicly` only when you
want public publication. It reviews changes, runs tests and package builds,
commits the intended changes, pushes an annotated `v<version>` tag, waits for the
Release action, downloads its packages, and verifies their SHA-256 checksums.
This executes real Git and GitHub operations; it is not a preview. It requires
GitHub authentication with permission to push and create releases, plus the
local build prerequisites described above. If local packaging tools are missing,
the prompt reports the limitation and verifies the action-built packages instead.

If `/release-build` does not work:

1. Press **Ctrl+Shift+P**.
2. Select **Chat: Run Prompt**.
3. Select **release-build**.

Alternatively, open the prompt file and select **Run Prompt**. Try **Developer:
Reload Window** if discovery has not refreshed. No commit or push is needed for
VS Code to discover the local prompt.

Both packages remain unsigned. The MSIX is intended for Store submission, not
ordinary direct installation without appropriate signing.

## Runtime Assets

`AuditFolderSettings` persists an optional selected directory in `%LOCALAPPDATA%\WindowsServiceAudit\settings.json`. `AuditDataDirectory.Resolve` validates that configured directory and uses it without silently falling back if it becomes unavailable. When no directory is configured, it probes the application directory and uses it when writable; otherwise, `MainWindow` supplies `%LOCALAPPDATA%\WindowsServiceAudit` as the fallback. Audit reports and `whitelist.csv` share the resolved location; the editor saves with the existing `Whitelist.Save` format, and each audit reloads it through `Whitelist.LoadOrSeed`.

The app packages the root README, `Copilot-Prompts.md`, and files under `docs/` as local Markdown help. The app uses WMI, the Windows registry, process metadata, and file-signing metadata locally. It does not invoke PowerShell, an online AI service, or network APIs.

## Recovery and Media Reproduction

Source, build scripts, the starter whitelist, icons, and reviewed trailer inputs
belong in Git. Generated packages and the final media belong in
[GitHub Releases](https://github.com/richardharris84/WindowsAudit/releases).
The recovery Release is a **draft archive of the existing builds**, not a new
Store-certified distribution. Draft assets require the owner's authenticated
GitHub access. It includes the unsigned Setup, unsigned Store MSIX, four logos,
the original final video and thumbnail, and `SHA256SUMS.txt`. It excludes private
reports, customized whitelists, and the test certificate/package.

To recover after losing the workspace, clone the repository, download the
recovery Release assets, and compare their SHA-256 hashes with `SHA256SUMS.txt`.
The original archived files are preserved byte-for-byte; rebuilding may produce
different bytes with equivalent behavior. Existing installer/MSIX files predate
later documentation changes; rebuild with a new package version before
distributing an updated application.

Regenerate Store logos from the versioned vector:

```powershell
pwsh -STA -File .\media\Build-Branding.ps1
```

Re-encode the final trailer from the versioned, normalized scene images:

```powershell
.\media\trailer\Build-Trailer.ps1 -FfmpegPath 'C:\tools\ffmpeg.exe' -FfprobePath 'C:\tools\ffprobe.exe'
```

Obtain FFmpeg/FFprobe separately (reference version: 9.0.2); they are not
committed. The script refuses to overwrite an existing final pair unless
`-Overwrite` is explicitly supplied. It checks the timeline, codecs, dimensions,
frame count, exact duration/start times, and full decoding. Its output is a
functional recreation, not a promise of the original MP4's checksum.
The exact final thumbnail is versioned as `media/trailer/thumbnail.png`.

The WPF harness can refresh the simulated app/menu scenes against current
source. Copy the archived frames to a disposable output directory first:

```powershell
New-Item -ItemType Directory .\artifacts\trailer-render -Force
Copy-Item .\media\trailer\frames\*.png .\artifacts\trailer-render
dotnet run --project .\media\trailer\harness\TrailerTools.csproj -c Release -- .\artifacts\trailer-render scan
dotnet run --project .\media\trailer\harness\TrailerTools.csproj -c Release -- .\artifacts\trailer-render
```

The harness verifies fictional REVIEW filtering and numeric sorting. Utility,
closing, and whitelist scenes can be reused from the archive; rerendering UI
does not recreate historical Windows utility screenshots or thumbnail artwork.
`Capture.ps1` and `Redact.ps1` are optional historical capture helpers requiring
explicit input/output directories and an interactive English Windows desktop.
They are not run by the trailer encoder. Captures contain real system data:
keep raw images private, and manually review every crop/redaction before use.
Raw captures and window-handle files are ignored. Pixelation alone must not be
assumed sufficient for a new screenshot; the reviewed final composite frames
are the recovery inputs committed here.

### Private Whitelist Backup

Run the backup helper with a private destination outside the workspace:

```powershell
.\tools\Backup-Whitelist.ps1 -Destination 'C:\Backups\WindowsAudit-Private'
```

It copies workspace, unpackaged LocalAppData, and known MSIX whitelist files
into a new timestamped folder and verifies their SHA-256 hashes. It also checks
saved audit-folder settings for configured whitelist locations. `manifest.json`
maps each copy to its original path. Pass `-AdditionalWhitelistPath` for any
other custom whitelist outside these locations.
Keep this unencrypted backup private; a cloud-synced destination follows that
provider's privacy and retention settings. Do not upload it as a Release asset.

To restore, close the app, choose the desired copy from the manifest, verify
its hash, then copy it to `whitelist.csv` in the active audit directory. Do not
replace a newer customization without checking it first. A backup outside the
workspace survives deleting that workspace, but separate/offline backups are
still advisable for protection against account loss or synchronized deletion.

[← Back to the README](../README.md)

---

Developed by: [Richard Harris](https://github.com/richardharris84)