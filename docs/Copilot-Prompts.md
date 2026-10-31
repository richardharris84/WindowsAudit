# Recreating Windows Audit with Copilot

An ordered prompt guide for rebuilding the PowerShell proof of concept, migrating it to the Windows Audit WPF application, and reproducing its packaging, Store materials, and testing workflow.

## Guide Sources and Scope

The project owner reports starting the PowerShell proof of concept on October 28, 2026, around 2 PM, then using it as the basis for this WPF application.

This guide was assembled by examining:

- The maintained files in the sibling `WindowServiceAudit-beta` workspace: `Evaluate-Services.ps1`, `README.md`, `whitelist.csv`, and the structure/schema of its generated Markdown and CSV reports.
- This workspace's application and core source, project and solution files, tests, documentation, icon resources, installer and MSIX scripts.
- The available conversation's user prompts and assistant implementation outcomes, including the reporting fixes, icon/startup fixes, trailer revisions, and installed-MSIX permission check.
- Existing branding, installer, MSIX, signed-test-package instructions, and final trailer deliverables. Generated service inventories are evidence of output shape, not application source or fixtures to redistribute.

**The original PowerShell creation prompt and initial WPF migration prompt were not recoverable from the available history.** Earlier sessions could not be recovered. The numbered prompts below are therefore **reconstructed, self-contained specifications**, informed by source and recovered requests; they are not presented as a verbatim transcript.

Prompts can reproduce the functionality and project structure, not guarantee identical generated code, certificate thumbprints, video bytes, or Store approval. Existing source and tests remain the reference when exact compatibility matters.

## How to Use This Guide

1. Work on Windows in new, empty folders. Do not overwrite the original prototype or application.
2. Set `BETA_ROOT` to a new `WindowServiceAudit-beta` folder and `APP_ROOT` to a new `WindowsAudit` folder. These names represent paths you supply to Copilot, not environment variables required by the app.
3. Give Copilot Prompt 00, then the numbered prompts in order. Review and verify each stage before continuing.
4. To recreate only the final WPF app, read the prototype prompts for context, then start implementation at Prompt 05. Do not add a PowerShell runtime dependency to the WPF app.
5. In a new Copilot session, provide this guide and the current source, summarize completed prompt numbers, and ask Copilot to inspect the files before continuing.
6. Store identity values below belong to this Windows Audit product. For a fork, obtain a separate reserved identity and replace the publisher, attribution, and branding as appropriate.
7. Downloads, installing tools, certificate creation/trust, app installation, publishing, and deletion are separate approval points. A prompt describing them is not permission to perform all of them automatically.

The final application and its usage are described by [README.md](../README.md), [FAQ](faq.md), and the [developer guide](development.md). The developer guide also documents the versioned media inputs, regeneration scripts, GitHub Release recovery archive, and private whitelist backup workflow.

## Prompt 00 - Establish the Working Contract

```text
Help me recreate Windows Audit, a read-only Windows service inventory and
reporting utility, using the ordered requirements in Copilot-Prompts.md.

Before editing, inspect the available source and documentation. Identify what
already exists, preserve unrelated work, and implement the requested stage
completely. Keep the implementation simple and use existing helpers and patterns.
Do not silently substitute success or empty reports when an operation fails.
Explain errors at the UI boundary; represent genuinely unavailable optional
Windows metadata as unavailable rather than guessing values.

All scans, whitelist matching, and report generation must run locally. Add no
advertising, built-in analytics, automatic uploads, network research, or online
AI dependency. Google links are optional user-initiated browser navigation.
Never implement automatic service stopping, disabling, deletion, reconfiguration,
application removal, startup toggling, or file cleanup.

Treat OK and REVIEW solely as whitelist path-text matches, never safety ratings.
Do not infer that a service is safe to disable from its name, signer, memory,
startup state, or absence from the whitelist.

Use synthetic test/demo records, not copies of this computer's inventory.
Never include credentials, signing private keys, private screenshots, account
identifiers, or machine-specific inventories in source or public assets.
Ask before installing tools, creating/trusting certificates, installing apps,
publishing, or deleting files. Keep test packages separate from Store packages.
Run focused validation after code changes. Report exactly what was verified
and what still needs human or installed-package testing.
```

## Phase A - Recreate the PowerShell Proof of Concept

These prompts reconstruct the prototype, including features that were not carried into the WPF application. Do not confuse prototype advisory classifications with whitelist status.

### Prompt 01 - Create the Read-Only Service Audit Script

```text
In BETA_ROOT, create Evaluate-Services.ps1 for Windows PowerShell 5.1 or later,
with README.md and whitelist.csv. Use built-in Windows facilities rather than
requiring runtime modules.

Support SampleSeconds (default 1, valid range 1-300), optional CsvPath, and
ShowAll. Use StrictMode Latest and terminating errors for required operations.
Read Win32_Service through CIM, service registry configuration, and available
Get-Service metadata. Do not change any Windows configuration.

Collect service name, display name, state, startup mode, service account,
process ID, command line, description, dependencies, and dependents where
available. Read delayed auto-start and trigger configuration presence from
the service registry key. Distinguish missing metadata from false values.

Produce structured records using the 24-column contract in Prompt 06, with no
CpuPercent output. Document the script parameters, read-only behavior, and
the limits of advisory classifications.
```

### Prompt 02 - Parse Paths and Apply an Editable Whitelist

```text
Implement testable helpers in the prototype for quoted and unquoted service
image paths. Separate executable candidates from arguments; detect malformed
quotes, missing paths, ambiguous unquoted paths with spaces, relative paths,
unresolved variables, and missing files. Preserve the original command line.
Expand environment variables and Windows SystemRoot forms for file lookup.

Resolve an executable target using the nonempty parsed executable path first
(independent of whether the file exists). Otherwise use Parameters\ServiceDll
with "(loaded by <host>)" when the registry ImagePath host is also parsed,
or the DLL alone. For a host alone, label the payload not identified; preserve
an unparseable registry command as such, or report Unavailable when absent.
Do not execute service binaries or turn ambiguity into a trust decision.

Require a valid whitelist.csv with a WhitelistTerm column and at least one
nonblank term. Match case-insensitive substrings against ExecutablePath or
ExecutableTarget. A match is OK; no match is REVIEW.

Use these starter terms, one per line after the WhitelistTerm header:
Adobe
system32
Battle.net
Google
Logi
Windows Defender
Edge
Steam
Microsoft.NET
WSL
Dropbox
Copilot
WINDOWS
Microsoft Shared
McmSvc
NetSetupSvc

These broad terms reproduce the starter data, not a recommended security
allowlist. Document their limitations.
```

### Prompt 03 - Add Memory Snapshots and Cautious Advisory Evidence

```text
After the selected sampling delay, take a process working-set snapshot in MB
for each distinct positive service PID. Shared service hosts share that PID's
reading; exited or inaccessible processes have unavailable readings. Do not
present the result as per-service attribution or a time average.

For prototype fidelity, use Get-AuthenticodeSignature for available executable
files and cache results by path. Keep signer/signature evidence separate from
whitelist status. The prototype's signature behavior is not the final WPF
certificate-only behavior described later.

Recognize the curated core names DcomLaunch, EventLog, LSM, PlugPlay, Power,
RpcEptMapper, RpcSs, SamSs, and SystemEventsBroker. Other classifications must
be advisory and explain their evidence and uncertainty.

Add review observations for automatic-start services currently stopped,
unquoted executable candidates containing spaces, and missing executable
files. A stopped automatic service may be normal or trigger-based.

Do not expose CPU usage in the report or CSV. The historical prototype has
internal resource-sampling code, but CPU is not a product feature to migrate.
```

### Prompt 04 - Recreate Prototype Reporting Without Machine Snapshots

```text
Create a RESULTS.md template headed "# Service Evaluation Summary" with
summary, reviewed-service, conclusion, and limitations sections. Include these
exact marker lines without the "Begin marker:" / "End marker:" labels:
Begin marker: <!-- BEGIN AUTO-GENERATED NON-WHITELIST REVIEWS -->
End marker: <!-- END AUTO-GENERATED NON-WHITELIST REVIEWS -->

Update only the block between those exact markers with current non-whitelisted
service reviews. Preserve everything outside it. If RESULTS.md or the markers
are absent, warn and skip the replacement rather than overwriting arbitrary
content. Do not use a previous computer's report as a permanent template.

Include available service description, paths, account/startup information,
dependency information, signature evidence, and cautious observations.
The prototype also investigated service DLLs, had curated role notes for
LSM/McmSvc/usbipd, and optional usbipd/firewall observations. If recreating
those legacy sections, label missing tools/data and use current observations,
not hardcoded conclusions from the original computer. These investigations
are not required WPF features.

Sort the prototype inventory by display name. ShowAll affects console
presentation only; CsvPath exports the full inventory. Explain that the
prototype console's manual-review count (signals/advisory category) is not
necessarily the same as the number of whitelist REVIEW records.

Document an example CSV export and the limitations of memory/signature data.
If adding Pester tests, create the tests before documenting their command.
The surviving beta README mentions tests\Evaluate-Services.Tests.ps1, but
that test file is not present in the surviving beta workspace.
```

## Phase B - Build the Final Native WPF Application

### Prompt 05 - Migrate to a Three-Project .NET Solution

```text
Use the PowerShell prototype as behavioral reference and create the native
Windows Audit application in APP_ROOT. Do not invoke PowerShell at runtime.

Create WindowsServiceAudit.slnx with:
- src\WindowsServiceAudit.Core: net10.0-windows class library.
- src\WindowsServiceAudit.App: net10.0-windows WPF WinExe referencing Core,
  AssemblyName WindowsAudit, Product Windows Audit, Version 1.0.2.0.
- tests\WindowsServiceAudit.Tests: net10.0-windows MSTest project referencing Core.

Enable nullable reference types and implicit usings. The reference dependency
versions are System.Management 10.0.0 and DocumentFormat.OpenXml 3.5.1 in Core,
and MSTest 4.0.2 in Tests. Use the existing MSTest/VSTest dotnet test workflow
and method-level test parallelization. Do not add unrelated frameworks.

Separate service acquisition, path parsing, folder resolution, whitelist
handling, CSV generation, Markdown generation, and DOCX generation into the
existing-style classes named in the source map below.

Keep the final app read-only with regard to Windows services. Do not port
prototype machine-specific recommendations, internal CPU calculations,
usbipd-specific probes, or firewall investigation into the WPF app.
```

### Prompt 06 - Preserve the Exact Inventory Contract

```text
Create a typed ServiceAuditRecord and AuditProgress(Message, Completed, Total).
The CSV columns must be exactly these 24 names in this order:
Status,Name,DisplayName,WorkingSetMB,State,StartMode,DelayedAutoStart,
TriggerConfigurationPresent,StartName,ReviewSignals,ExecutablePath,
PathCandidates,PathStatus,UnquotedPathWithSpaces,ExecutableTarget,CommandLine,
Description,Category,ClassificationEvidence,ProcessId,Dependencies,Dependents,
SignatureStatus,Signer

WorkingSetMB is nullable double, ProcessId is int, DelayedAutoStart and
TriggerConfigurationPresent are nullable bool, and UnquotedPathWithSpaces is
bool. Other fields are strings. Unavailable memory must not become zero.
Keep the model independent of WPF.

Port the parser and whitelist behavior using testable Core helpers. Preserve
the exact target fallback described in Prompt 02, environment expansion,
unquoted-path observations, original command line, and unavailable/malformed
path states. A nonempty parsed executable remains the target even if missing.

Keep Dependents in the compatibility schema. The reference WPF registry reader
does not populate reverse dependencies; leave it empty when unavailable.
Do not claim that the WPF app builds a complete reverse-dependency graph.
```

### Prompt 07 - Implement Writable Audit Data and Whitelist Persistence

```text
Implement AuditDataDirectory, Whitelist, and AuditFolderSettings.

Honor an existing optional AuditDirectory setting in
%LOCALAPPDATA%\WindowsServiceAudit\settings.json. Validate a configured
directory and its writability. If invalid or unwritable, report the error;
do not silently switch to another folder.

With no configured directory, probe AppContext.BaseDirectory and use it when
writable; otherwise use the logical fallback
%LOCALAPPDATA%\WindowsServiceAudit. Reports and whitelist.csv share this
resolved folder. Probe writability using a temporary file and remove it.
Do not assume the installation directory is writable.

Seed whitelist.csv only if it does not exist. Use the exact starter terms in
Prompt 02. Never overwrite user edits. Require WhitelistTerm and at least
one term; trim and deduplicate terms case-insensitively when saving. Match
case-insensitive substrings in executable path or target.

Retain compatibility with saved output-folder preferences even though the
final UI has no Configure output folder menu. Do not confuse manually chosen
CSV/DOCX export destinations with the automatic audit-data directory.

Document that MSIX may virtualize the logical LocalAppData path into a
package-private LocalCache location. Do not infer missing reports merely
because the unpackaged physical path is absent.
```

### Prompt 08 - Implement the Native Local Audit Pipeline

```text
Implement AuditRunner.RunAsync(outputDirectory, Seconds=1, progress,
cancellationToken), validating the directory and 1-300 second interval.

Read Win32_Service using System.Management and dispose WMI objects. Query Name,
DisplayName, State, StartMode, StartName, ProcessId, PathName, and Description.
Read optional metadata under HKLM\SYSTEM\CurrentControlSet\Services\<name>:
ImagePath, Parameters\ServiceDll, DelayedAutoStart, TriggerInfo presence,
and DependOnService. Access restrictions on optional metadata must not become
a false audit success with invented values.

Load/seed the whitelist, parse paths, resolve targets, and assign OK or REVIEW.
Add the stopped-auto-start, unquoted-spaces, and missing-file observations.
Keep observations distinct from whitelist status and advisory classification.

For publisher evidence, use embedded certificate extraction with
X509CertificateLoader.LoadCertificateFromFile. Report
"Signer certificate present; trust not evaluated" when found, no embedded
certificate when absent, and unavailable/not checked where appropriate.
This is NOT full Authenticode validation, trust-chain validation, reputation,
or a guarantee of authenticity. A Microsoft-looking subject is not proof
that the service is essential or optional.

Use the curated core-name list from Prompt 03. Otherwise classify cautiously
by the presence of Microsoft/other signer information or unknown publisher.
Include evidence explaining that trust was not evaluated.

Read inventory, wait the selected interval asynchronously, then capture each
unique PID's WorkingSet64 once, converted with 1024*1024 and rounded to one
decimal MB. Propagate shared-host readings and preserve unavailable values.
No CPU percentage or continuous monitoring.

Report progress for reading inventory, waiting, capturing working sets, and
writing reports. Support cancellation internally and release resources.
Return REVIEW first, then display name alphabetically within status groups.
Write Win-Srv-Eval-Results.csv and Win-Srv-Eval-Report.md for this run.
```

### Prompt 09 - Generate Safe, Current CSV and Markdown Reports

```text
Implement CsvReportWriter with the exact Prompt 06 header order, UTF-8 BOM,
quoted fields and doubled embedded quotes, invariant numeric formatting,
True/False bool values, and empty values for unavailable metadata.
Always export the full supplied inventory, REVIEW first and then alphabetical
display name within status groups; UI filtering must not silently truncate it.

Implement ResultsWriter so all counts and detailed REVIEW entries come from
the current inventory. Include descriptions under "What this service does",
state/startup/account information, executable and resolved paths, path
observations, memory when available, signer evidence, dependencies where
available, and explicit limitations.

Include inventory, OK, REVIEW, running, and automatic-start counts plus a
generation timestamp. Order detailed REVIEW sections by service name.
Create a complete report when the output file is absent. If its first title
is "# Windows Service Audit Results" or "# Service Evaluation Summary",
regenerate the whole report, replacing stale examples and summaries.
For an unrelated custom report with the two exact generated-section markers
from Prompt 04, replace only that section and preserve the surrounding text.
For an unrelated unmarked file, leave its bytes unchanged and raise an error.
Write Markdown as UTF-8 without a BOM.

If there are no REVIEW services, say so and remove stale generated entries.
Escape untrusted service metadata so punctuation does not corrupt Markdown.
Create optional https://www.google.com/search?q= links using URI-escaped
"DisplayName (Name)" queries. Labels identify the service and do not start
with "Search for ". Do not request Google results or promise an AI Overview.

Do not reuse the beta's old services, conclusions, or counts as static content.
```

### Prompt 10 - Recreate the Main Window and Final Menus

```text
Build the WPF main window with title "Windows Audit" and the in-content heading
"Windows Service Audit". Use a 1500x820 default size, minimum 980x620, readable
Segoe UI typography, a pale canvas, bordered content panels, and these colors:
canvas #F2F5F5, ink #1C3038, muted #60747B, accent #087E83, line #D9E2E3.

Provide main actions Scan Windows Services, Whitelist, Export CSV, and About;
service/review counts; sample-seconds choices 1,3,5,10,30; a text filter;
progress/status feedback; and a read-only sortable service DataGrid.
Bind grid columns to the typed model and show service and path metadata.
Auto-generate model columns but cancel UnquotedPathWithSpaces and Dependents
in the grid only; preserve both fields in the model and CSV schema.
Prevent row addition/deletion, permit
column sorting/reordering, use single selection and horizontal grid lines.
Use friendly headers for Memory (MB), Service Account, Executable Path,
Executable Target, Path Candidates, Path Status,
Delayed Auto Start, Trigger Configuration, Classification Evidence, Process ID,
Review Signals, and Signature Status. Keep other property names as headers.
Call the working-set column "Memory (MB)", not "WorkingSet (MB)".

Provide unique Alt access keys for menu siblings and main-window buttons:
Alt+F/V/T/H for menus, Alt+S/W/E/G for scan/whitelist/CSV/getting started,
Alt+C for the sample interval, and Alt+L for the filter. Use WPF underscore
mnemonics and targeted Labels for input controls. Supply clear automation names
without decorative glyphs. Give dialog buttons access keys and Escape-to-close
or cancel behavior; the multiline whitelist editor must let Tab leave the field.
Use dynamic Windows system colors throughout windows and Markdown content,
and a strong keyboard-focus outline. Offer session-wide 100-200% interface zoom
in 25% steps using View menu items and Ctrl+Plus/Minus/0 in all windows.
Keep enlarged controls reachable using bounded layouts and horizontal scrolling.
Raise polite UI Automation live-region events for scan start, completion counts,
and failure, not for every progress update. Gate these enhancements, including
added button/input shortcuts and automation labels, behind a checkable View >
Enhanced accessibility option, off initially and remembered across restarts in
accessibility.json separately from audit-folder settings. Menu shortcuts remain
available for everyone. Disabling the option restores standard styling and
100% zoom. Surface preference read/write errors explicitly.
Validate with automated UI tests and
a manual Narrator/NVDA, contrast-theme, and 200% Windows display-scaling checklist.

Use this final menu structure:
File: Edit whitelist..., separator, Export CSV..., separator, Exit.
View: Results (initially disabled).
Tools: Scan Windows Services..., Stop / Disable Services, separator,
       Add or remove programs, Startup Apps, separator, Cleanup Large Files.
Help: Getting Started (opens README), separator, About (shows four-component
assembly version and Built by Richard Harris). The toolbar Getting Started
button opens README; do not hardcode the About version.

Do not expose Configure audit folder/Configure output folder in the final menu.
Preserve the saved-setting compatibility underneath it. Scan Windows Services
must be the first Tools item, not a File command.

The final scan-complete footer says "Audit complete." without displaying the
application or output-folder path. Paths remain available in relevant saved
file confirmations, not permanently in the main window footer.
```

### Prompt 11 - Wire Scanning, Filtering, Sorting, and Results State

```text
Wire the UI to the Core runner without freezing the window during the wait.
Display operation errors clearly and restore controls in a finally path.
Populate the grid and counters after successful completion, then open the
current report in the Results dialog.

View > Results starts disabled even if an old report exists. Disable it for
a new scan and keep it disabled after a failed scan; enable only after a
successful scan in this session. Reopening Results reads the active report.

Apply case-insensitive text filtering across status, service name/display
name, state, startup mode, executable path/target, category, observations,
description, and signer. Refresh the collection view when the text changes.
Do not change underlying export inventory when applying a visual filter.

Default grid order is REVIEW first, then alphabetical display name within
each group. User-selected column sorts may replace that default view order.
Memory (MB) must sort by the nullable numeric WorkingSetMB property, never
formatted strings; test both ascending and descending with 2,10,100 and blanks.
CSV exports retain the required REVIEW-first order.
```

### Prompt 12 - Build the Whitelist Editor and Local Markdown Dialog

```text
Create WhitelistEditorWindow (620x680, minimum 480x440) with one term per line,
Save and Cancel, and validation through the shared Whitelist helpers. Trim,
remove blank lines, and deduplicate terms case-insensitively in the editor;
prevent saving an empty list. Do not permanently display the whitelist path.
Only after a successful save, show a confirmation with the actual saved
location. Reload edited terms on the next scan.

Create ReadmeWindow (860x720, minimum 600x480) for startup welcome, Help > About,
local documentation, and Results. Center dialogs on their owner.
Show README on startup, with OK to continue; About reopens it.
Support headings, paragraphs, ordered/unordered lists, bold/italic, inline
and fenced code, links, and Markdown horizontal rules. Hide HTML marker lines.
Render a real divider above the attribution; do not treat "---" as text.
Scroll each newly opened document to the top.

Navigate relative local Markdown links within the supplied assets root only.
Reject traversal outside that root and unsupported local file types. Allow
user-initiated http/https/mailto links via the shell, without prefetching.

Package README.md and docs\*.md (including docs\Copilot-Prompts.md) under Assets,
preserving relative paths. In docs\development.md link Copilot-Prompts.md near the top;
verify this also works in the app rather than only in a source editor.
```

### Prompt 13 - Add CSV and Compatible DOCX Export

```text
Wire Export CSV to a SaveFileDialog, default name Win-Srv-Eval-Results.csv.
Add Export DOCX only to Results dialogs, not the startup/About help dialog.
Export the currently displayed report using default name
Win-Srv-Eval-Report.docx. Both save dialogs initially point to
AppContext.BaseDirectory and permit a different, writable destination.

Use DocumentFormat.OpenXml, not installed Microsoft Word or external
conversion processes. Preserve supported report headings, paragraphs,
bold/italic, inline/fenced code, lists, and clickable links. Hide generated
HTML markers. Escape text correctly and provide page/style information.
Surface export errors, and show success only after the file is written.

Prevent the compatibility regression where Word-like ZIP validation succeeds
but OpenOffice cannot open the file. The package-level officeDocument
relationship must target relative "word/document.xml", not an absolute
"/word/document.xml" URI. Inspect the package relationship, run OpenXML
validation, and open a newly exported document in OpenOffice Writer when
available. Previously exported broken files are not fixed by rebuilding.
```

### Prompt 14 - Wire All Windows Utility Shortcuts

```text
Implement user-initiated shortcuts without performing the utilities' actions:
- Stop / Disable Services -> services.msc using shell execution.
- Add or remove programs -> ms-settings:appsfeatures.
- Startup Apps -> ms-settings:startupapps.
- Cleanup Large Files -> a new File Explorer window at C:\, or
  shell:MyComputerFolder when C: is unavailable, searching for size:>10MB.

For the Explorer behavior, launch a fresh window (/n), identify a newly
created CabinetWClass window, use UI Automation to locate its Search edit
control, set its ValuePattern to size:>10MB, focus it, and submit Enter.
Bound window/search discovery to about 15 seconds and report failure.
Do not type into an unrelated existing Explorer window or require deprecated
.search-ms assets. Keep Windows UI/language compatibility limits explicit.

Opening these utilities must not stop/disable services, uninstall software,
toggle startup entries, or delete files. Test only opening and searching.
Document the danger of changing services and keep any manual stop/disable
instructions after review and export in the Getting Started sequence.
```

### Prompt 15 - Recreate the Transparent Broom Identity

```text
Create Assets\Broom.xaml with a clearly recognizable golden fan-shaped broom,
brown handle/band, bristle detail, and six white sparkles on a transparent
background. Generate a multi-resolution WindowsAudit.ico with 16,24,32,48,
64,128,256 pixel images. Use it for ApplicationIcon and individual windows.
Make the same icon appear in the executable, main window, help/results,
whitelist editor, installer, and shortcuts.

Do not globally override Window.IconProperty metadata. That caused a startup
crash ("PropertyMetadata is already registered for type 'Window'") during
development. Set icons on the relevant windows/resources instead.

Validate actual icon resources and alpha transparency at each size, then
launch the built application and confirm an interactive main window.
Distinguish old Explorer icon-cache entries and title-bar selection backgrounds
from incorrect embedded artwork. Prefer a targeted shell refresh notification
before suggesting sign-out; do not delete broad cache folders or kill Explorer.
The final icon is transparent, not the earlier teal or blue-background design.
```

### Prompt 16 - Recreate the User and Developer Documentation

```text
Create README.md, docs\faq.md, and docs\development.md, keeping
them consistent with implemented behavior and packaged local help.
Keep Getting Started brief. Place detailed usage, report interpretation, file
locations, and whitelist guidance in README above Privacy and Safety, without
repeating the quick-start steps. Do not create a separate docs\usage.md.

Explain startup/About, scanning/sample delay, filtering and numeric memory
sorting, REVIEW-first ordering, CSV export, Results/DOCX, whitelist editing,
logical audit-folder fallback, legacy saved-folder preferences, and the final
Tools commands. Explain full report regeneration versus protected custom files.

Warn that OK/REVIEW are only case-insensitive path-text matches; memory is a
shared-host process snapshot; embedded certificate presence is not evaluated
signature trust; unavailable metadata is not evidence of wrongdoing.
State that reports omit CPU usage and Google opens only on an explicit click.
Reports can contain account names, identifying paths, and command lines.

In README Getting Started, put review and export before the optional step
opening Services. Explain manual Stop and Properties > Startup type > Disabled
only with cautions about dependencies and consequences.

Document source prerequisites, solution layout, build/test/run/publish commands,
installer and MSIX generation, and installed-package testing. Do not document
nonexistent .search-ms files or a fabricated source repository URL. The current
source repository is https://github.com/richardharris84/WindowsAudit.
Link Copilot-Prompts.md from the top of the developer guide.

End README and development.md with a horizontal rule and:
Developed by: [Richard Harris](https://github.com/richardharris84)
Ensure those dividers render inside the app.
```

### Prompt 17 - Add Regression Coverage and Validate the Release

```text
Add deterministic MSTest regression tests using temporary folders and fictional
records, not live service expectations. Recreate the reference regressions
and check these acceptance cases:
- Whitelist load/seed/save, required header/nonempty terms, trimming and
  case-insensitive matching against either path or resolved target.
- Quoted/unquoted/malformed image paths, environment expansion, target fallback.
- Audit-directory default/fallback/configured behavior and explicit configured
  path failure.
- Exact CSV header/schema, escaping, nullable values, invariant numbers, and
  REVIEW-first then alphabetical order.
- Current inventory-driven Markdown summaries and every REVIEW service;
  zero-review runs; removal of old examples; generated-section preservation;
  refusal to overwrite unrelated unmarked content; escaped Google links and
  labels without "Search for ".
- DOCX structure, content, hyperlinks, OpenXML validation, and relative root
  document relationship needed for OpenOffice.

For the reference VSTest configuration, use:
dotnet build .\WindowsServiceAudit.slnx -c Release
dotnet test .\WindowsServiceAudit.slnx

Verify WindowsAudit.exe is built under the app's bin\Release\net10.0-windows.
Perform a UI smoke test for startup, report gating, filtering, numeric sorts,
whitelist saving, exports, and tool launch behavior. Do not claim unit tests
prove installed MSIX behavior, Store approval, or service safety.
```

## Phase C - Distribution and Installed-Package Testing

### Prompt 18 - Create a Self-Contained Setup Program

```text
Add installer\Build-Installer.ps1 and installer\WindowsAudit.iss.
Use Inno Setup 6 (reference tool version 6.7.3), locating its compiler or
accepting -IsccPath. Ask before downloading/installing missing tools.

Publish the WPF app Release win-x64, self-contained, PublishSingleFile=true,
IncludeNativeLibrariesForSelfExtract=true, DebugType=None, DebugSymbols=false.
Include all required help/whitelist assets and use the transparent icon.
Do not assume a framework-dependent build's EXE alone is distributable.

Build artifacts\installer\WindowsAudit-Setup.exe. Install per-user under
{localappdata}\Programs\Windows Audit, PrivilegesRequired=lowest,
x64compatible. Add a Start menu shortcut, optional desktop shortcut, and
registered uninstaller. Reference AppId:
E45F0B73-903B-46B6-994A-3419AB425E91.
Do not explicitly purge existing user-generated reports or settings.

With approval, install to a temporary test location, verify shortcut creation,
launch the app, and uninstall the test installation. Do not touch unrelated
existing installations. Record what was actually tested.

Explain that this Setup is unsigned unless separately signed. Public signing
sequence: publish, sign application, build Setup, sign Setup, verify signatures,
then calculate SHA-256. Protect private keys and use trusted timestamping.
Signing does not guarantee immediate removal of SmartScreen warnings.
```

### Prompt 19 - Create the Microsoft Store MSIX

```text
Add packaging\AppxManifest.xml and packaging\Build-MSIX.ps1 for a self-contained
x64 WPF desktop package. Use the existing product's reserved identity:
Name: BuiltbyRichardHarris.WindowsAudit
Publisher: CN=2CDAA861-DB03-4AFA-B278-C278B732D5D4
PublisherDisplayName: Built by Richard Harris
DisplayName: Windows Audit
Product ID (Partner Center reference): 9NV87HQN321L
Current package and executable version: 1.0.2.0
Minimum Windows version: 10.0.19041.0
Executable: WindowsAudit.exe
EntryPoint: Windows.FullTrustApplication
Restricted capability: runFullTrust

These are product identity values, not signing credentials. A fork needs
its own reserved values; do not guess an identity.

Run the build in STA, locate MakeAppx from PATH/Windows SDK or
-MakeAppxPath, and support tools extracted under artifacts\tools\sdk-buildtools.
Ask before obtaining SDK tools; the reference used Microsoft SDK BuildTools
10.0.28000.2957. Avoid requiring a particular SDK download forever.

Use a unique staging directory, publish self-contained win-x64 with the same
release flags as the installer, rasterize Broom.xaml into package logos at
50x50,44x44,150x150, inject the requested four-part version, run MakeAppx pack
with validation, and remove only the staging files created by this build.
Require version form x.x.x.0; increment it for later Store submissions.

Create artifacts\msix\WindowsAudit_1.0.2.0_x64.msix unsigned for Partner Center.
The Store signs its accepted distribution package, not the separate EXE Setup.
Do not claim MakeAppx validation is installed-functionality testing or Store
certification. Do not upload a locally test-signed copy as the Store artifact.
```

### Prompt 20 - Prepare an Isolated Signed MSIX Test Copy

```text
Explain local testing choices first. An unsigned Store-submission package
normally cannot be installed by double-clicking; do not promise Developer Mode
alone makes that work.

With explicit approval, prepare a separate test copy under artifacts\msix-test.
Leave the original unsigned MSIX unchanged and record its SHA-256 before/after.
Create a short-lived (reference: 90-day) nonexportable RSA-2048/SHA-256 code
signing certificate whose subject exactly matches the package publisher.
Sign only WindowsAudit_1.0.2.0_x64_TEST.msix and export only the public
WindowsAudit-TEST.cer. Do not export or commit a private key/PFX.

Verify signing cryptography and that original application payload entries
are unchanged apart from required package-signature/content-type metadata.
Remove the temporary personal-store certificate/private key after successful
signing when no longer needed, identifying it by its exact new thumbprint.
Do not trust the certificate or install the app as part of package preparation.

Write TESTING.txt explaining installation, the workflow checklist, uninstall,
and precise certificate cleanup. Do not hardcode a previous test certificate's
thumbprint or expiry into a newly generated package's instructions.
```

### Prompt 21 - Guide Testing of the Actually Installed MSIX

```text
Guide me step by step, preferably on a spare Windows PC or VM. Ask before
certificate trust changes or installation. For this self-signed test workflow,
import only its public certificate into Local Machine > Trusted People, not
Trusted Root Certification Authorities. Explain the scope of this temporary
trust and how to remove that exact certificate afterward.

Install the signed TEST.msix through Windows App Installer. Close any
automatically launched instance, then launch Windows Audit from Start, not
bin\Release or an old pinned EXE installation.

Check startup/About and OK; complete a scan; filter by a service and REVIEW;
sort Memory (MB) both ways; confirm View > Results becomes available;
export CSV and DOCX to Documents and open them; edit/save/reopen the whitelist;
open every Tools utility without changing settings/deleting files; close,
restart from Start, and scan again. Check automatic reports actually exist.

Account for MSIX virtualization. For the tested identity, files were physically
observed under:
%LOCALAPPDATA%\Packages\BuiltbyRichardHarris.WindowsAudit_ax5km49db1rye\LocalCache\Local\WindowsServiceAudit

The logical fallback remains %LOCALAPPDATA%\WindowsServiceAudit; do not require
that unpackaged physical folder to exist. Discover/verify the actual package
family for a fork rather than hardcoding this suffix universally.
Confirm CSV and Markdown existence without unnecessarily exposing their data.
Also respect a previously configured audit directory.

After testing, guide uninstall and removal of only this test certificate from
Trusted People. Keep the original unsigned MSIX for Partner Center.
Normal path virtualization is not a reason to rebuild/reupload. Actual source
or content changes after a package was built require a new versioned build
if those changes are intended for distribution.
```

## Phase D - Store Listing, Branding, and Trailer

### Prompt 22 - Prepare Accurate Store Text and Certification Notes

```text
Inspect source and docs before drafting Store fields. Produce a description,
feature entries, initial-release notes, short description, keywords, privacy
policy draft, runFullTrust justification, and certification testing notes.
Do not invent sign-in, purchases, accessibility certification, pen/ink,
generative AI, or hardware requirements the app has not implemented/tested.
Check current Partner Center requirements rather than assuming old UI labels
or account/signing rules are unchanged.

Briefly represent every feature, especially all utility shortcuts:
- Local Windows service inventory with sortable columns and text filtering.
- REVIEW first in the inventory, with detailed review reports.
- Descriptions, startup configuration, accounts, and executable paths.
- Process memory snapshots with numeric sorting.
- Embedded publisher certificate information and executable-path observations.
- Editable case-insensitive text-match whitelist.
- CSV inventory and DOCX report export.
- Detailed reports with optional Google Search links.
- Windows Services, Installed apps, and Startup Apps shortcuts.
- File Explorer search for files larger than 10 MB.
- Local processing without advertising or built-in analytics.

Say Windows 10 version 2004 or later and Windows 11, x64, built with WPF.
Clearly state OK/REVIEW are text matches, not safety ratings; the app never
directly stops, disables, deletes, or reconfigures services. Users make any
changes separately through Windows utilities and must understand consequences.

Privacy: service accounts, paths, command lines, and descriptions may contain
personal/identifying information even though processing is local. Do not say
"no personal information is used". Explain local reports/settings/whitelist,
no automatic inventory uploads, no advertising or built-in analytics, explicit
Google links using display name/service name, and reviewing exports before
sharing. Cloud-synced/network export folders may transfer files through the
user's chosen storage service. Do not invent a public policy URL or contact.

runFullTrust: WPF desktop execution, WMI/registry/process/file metadata,
local reports, and explicit Windows utility launches; no automatic elevation
or built-in modification of services/settings/files.

Certification notes: no account/subscription/license key, startup/About,
scan, filter/sort, Results, exports, whitelist, all Tools entries, and expected
unavailable metadata/folder behavior. Separate package validation, installed
testing, and Microsoft's certification. Guide completion of required fields
and final review; do not submit/publish without my approval.
```

### Prompt 23 - Generate Exact-Sized Store Branding

```text
Render branding from the final transparent broom-and-six-sparkles icon,
preserving transparency, aspect ratio, and a recognizable silhouette.
Create under artifacts\branding:
WindowsAudit-Logo.png (1024x1024)
WindowsAudit-Logo-300x300.png (300x300)
WindowsAudit-Logo-150x150.png (150x150)
WindowsAudit-Logo-71x71.png (71x71)

Inspect actual PNG pixel dimensions and alpha, not just filenames or an
image-generation request. Use the vector source or a high-quality render;
avoid accidentally distributing an older blue-background version.
```

### Prompt 24 - Create the Final 60-Second Store Trailer

```text
Create a captioned, silent-in-content Windows Audit trailer using the actual
WPF interface with fictional service records. The final deliverables are:
artifacts\trailer\WindowsAudit-Trailer-60s-Store.mp4
artifacts\trailer\WindowsAudit-Trailer-60s-Thumbnail.png

Show startup/About first, not an already-open Tools menu. Give scanning a
substantial showcase: ready state, selected sampling delay, staged progress,
completed inventory, typed REVIEW filtering, numeric Memory (MB) sorting,
service details, whitelist editing, and report/DOCX/CSV capability captions.
Use the actual view/filter/sort implementation in a separate capture harness
where practical; mark fictional data as demonstration data.

Demonstrate every Tools entry, including Scan Windows Services, Services,
Installed apps, Startup Apps, and Cleanup Large Files. With approval, show
real Windows utilities but do not change settings or delete files. Conceal
usernames, personal paths, installed-software details, and real filenames.
For Explorer, show an actual C:\ size:>10MB search header, not a demo-folder
listing with a private capture path. Crop/redact private results.

Use this 60-second storyboard:
00-04 About/startup
04-06 Ready to scan
06-08 Sample interval
08-14 Four scan stages, 1.5 seconds each
14-18 Completed inventory
18-22 Type REVIEW and hold filtered results
22-26 Numeric memory sorting
26-29 Service details
29-32 Whitelist
32-37 Detailed report and export capabilities
37-42 Services menu and utility (0.8 + 4.2 seconds)
42-46 Installed apps menu and utility (0.8 + 3.2)
46-50 Startup Apps menu and utility (0.8 + 3.2)
50-56 Cleanup Large Files menu and Explorer search (0.8 + 5.2)
56-60 Closing title and local/read-only message

Use fictional categories that do not accidentally all contain the word
"review"; verify REVIEW filtering leaves the intended two unmatched records.
For offscreen WPF captures use explicit layout sizes, avoid VisualBrush
AutoLayoutContent distortion, and flush DataBind/dispatcher work before
capture so grid cells are populated.

Normalize scene PNGs to RGB24 before concat/encoding. Mixed PNG pixel formats
previously caused missing scenes and incorrect timing.

Check current Microsoft Store trailer requirements. The verified reference
encoding was MP4 1920x1080, progressive 30 fps, 1800 frames, H.264 High
level 4.1, avc1, yuv420p, target 50 Mbps CBR-style, closed GOP every 15 frames,
two consecutive B frames, CABAC, and faststart. Include a silent AAC-LC stereo
48 kHz track configured for 384 kbps, despite no narration/music.
Silent AAC may have a much lower measured bitrate; do not add audible noise.

Reference FFmpeg video flags:
-c:v libx264 -preset medium -profile:v high -level:v 4.1 -pix_fmt yuv420p
-b:v 50M -minrate 50M -maxrate 50M -bufsize 50M -g 15 -keyint_min 15 -bf 2
-sc_threshold 0
-x264-params nal-hrd=cbr:filler=1:force-cfr=1:open-gop=0:b-adapt=0:cabac=1
-tag:v avc1

Use a silent anullsrc stereo 48000 input and AAC encoding. Reference timing
flags were -af atrim=duration=59.978666667 -t 60
-movflags +faststart+negative_cts_offsets -use_editlist 0
-avoid_negative_ts disabled. They produced 60.000-second audio/video/container
durations starting at zero; verify rather than assuming flags always do so.

Create a new thumbnail based on the final trailer, exactly 1920x1080 PNG.
Check actual dimensions; an image model previously returned 1672x941/940
despite the requested aspect ratio.

Verify streams, frame count, duration, keyframe/GOP structure, full decoding,
and representative frames from every scene, not merely that the video plays.
Only after the final pair passes, and with explicit deletion approval, remove
named deprecated files in artifacts\trailer so that folder contains only the
latest video and thumbnail. Do not delete branding, packages, or broad folders.
```

### Prompt 25 - Finish With a Reproducibility and Release Audit

```text
Compare the reconstructed project against every requirement in this guide.
Confirm source, tests, docs, packaged help, icons, installer/MSIX scripts,
and requested Store assets are present and mutually consistent.

Run focused tests/builds for any final code changes. Check local documentation
links both in source and packaged Assets. Verify no real inventory, private
capture, signing private key, or machine-specific report was made a fixture.
List tool prerequisites and remaining human checks explicitly.

Do not equate an unsigned Store package with an installable public release.
Do not upload the test-signed package, auto-trust a certificate, or submit to
Partner Center without approval. Use a new package version when distributing
changes made since the last build. Do not rebuild just because normal MSIX
virtualization changed where files were physically observed.

Summarize what was built, validated, installed/tested, and not yet certified.
Keep Copilot-Prompts.md and the developer guide current with the final result.
```

## Source Map and Important Differences

| Area | Reference files or deliverables |
|---|---|
| PowerShell prototype | Sibling `WindowServiceAudit-beta`: `Evaluate-Services.ps1`, `README.md`, `whitelist.csv`; generated `RESULTS.md` and `service-evaluation-results.csv` show legacy output shape |
| Solution and dependencies | `WindowsServiceAudit.slnx`; the three `.csproj` files under `src` and `tests` |
| Native inventory and model | `src/WindowsServiceAudit.Core/AuditRunner.cs`, `ServiceAuditRecord.cs` |
| Path and whitelist behavior | Core `ServicePathParser.cs`, `Whitelist.cs`, `AuditDataDirectory.cs`; app `AuditFolderSettings.cs` |
| Reporting | Core `CsvReportWriter.cs`, `ResultsWriter.cs`, `DocxReportWriter.cs` |
| UI and tools | App `App.xaml`, `App.xaml.cs`, `AssemblyInfo.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `ReadmeWindow.cs`, `WhitelistEditorWindow.cs` |
| Runtime assets | App `Assets/Broom.xaml`, `WindowsAudit.ico`, `whitelist.csv`; README, this guide, and `docs/*.md` copied under output `Assets` |
| Regression tests | `tests/WindowsServiceAudit.Tests/AuditCoreTests.cs`, `MSTestSettings.cs`, project file |
| EXE setup | `installer/Build-Installer.ps1`, `WindowsAudit.iss`; generated `artifacts/installer/WindowsAudit-Setup.exe` |
| MSIX | `packaging/Build-MSIX.ps1`, `AppxManifest.xml`; generated `artifacts/msix/WindowsAudit_1.0.2.0_x64.msix` |
| Local signed test | Generated `artifacts/msix-test/*_TEST.msix`, public `.cer`, and `TESTING.txt`; not public-distribution credentials |
| Store imagery and video | Generated `artifacts/branding/*.png`; the final two files under `artifacts/trailer` |

Important boundaries when recreating the final application:

- **Prototype versus WPF:** the prototype uses PowerShell and Authenticode checks; WPF uses native .NET acquisition and embedded signer-certificate extraction without evaluating trust.
- **Reports:** the beta updates only a marked block in an existing report. WPF also creates/regenerates recognized reports from the current inventory, protecting unrelated custom files.
- **Names:** `RESULTS.md`, `service-evaluation-results.csv`, `Run audit`, `WorkingSet (MB)`, and the visible output-folder menu are historical names, not the final UI/output targets.
- **Dependency metadata:** `Dependents` remains in the compatibility/CSV schema but is hidden from the WPF grid; its registry reader leaves it empty. Do not advertise complete reverse-dependency analysis.
- **Explorer:** the final command drives a fresh search through UI Automation; it does not depend on saved `.search-ms` files.
- **Trailers:** 36-second and 43-second videos and older thumbnails were intermediate outputs. The final target is the verified 60-second pair.
- **Testing:** source/unit tests, MakeAppx validation, installed-MSIX workflow checks, and Store certification are different gates.
- **Package freshness:** adding this guide to runtime assets changes future build content; already-created MSIX/Setup artifacts are not retroactively updated.

[Back to the developer guide](development.md)
