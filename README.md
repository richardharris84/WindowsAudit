# Windows Audit

Windows Audit helps you review the services installed on your Windows computer. It gathers service details into a sortable table and creates reports you can keep or share with a support technician.

The audit only reads service information. It does not start, stop, disable, or change Windows services. Its `OK` and `REVIEW` labels are based on text matches against your whitelist; they are not safety recommendations. Read [Understand the Results](#understand-the-results) before acting on a result.

Windows Audit is designed for Windows 10 and Windows 11, x64.

## Getting Started

Run **WindowsAudit-Setup.exe** to install Windows Audit, then open it from the
**Start menu**. Setup includes .NET, so no separate runtime installation or ZIP
extraction is needed. A desktop shortcut is optional.

1. Open Windows Audit. The welcome window includes this guide; choose **OK** to continue.
2. Choose a sample interval, then choose **Scan Windows Services**.
3. Review the services and use the filter to find entries of interest.
4. Choose **Export CSV** to save a copy of the inventory.
5. After reviewing the results, choose **Tools > Stop / Disable Services** to open the Windows Services utility. Select a service and choose **Stop** to stop it, or right-click it, choose **Properties**, and change **Startup type** to **Disabled**.

## Using the App

### Scan and Review the Inventory

The app reads the service inventory, waits for the selected sample interval in seconds, then captures process working-set readings. By default, `REVIEW` services appear first, with services sorted alphabetically by display name within each status group. CSV exports use this same order. Click a column heading to sort, or enter text in the filter to narrow the visible rows.

### Reports and Exports

The results dialog includes **Export DOCX** to save the displayed `Win-Srv-Eval-Report.md` report as a Word document, including support for opening it in OpenOffice Writer. If you close the results dialog, choose **View > Results** to reopen the report from the active audit folder. **View > Results** stays disabled until a scan completes successfully in the current session, and is disabled again while a new scan runs or if it fails.

Both **Export CSV** and **Export DOCX** start in the application folder; you can choose a different folder before saving. CSV export saves the loaded inventory, not just the rows visible through the filter.

Each audit writes `Win-Srv-Eval-Results.csv` and regenerates `Win-Srv-Eval-Report.md` from that run's inventory. The report summary and every detailed `REVIEW` entry therefore reflect the services actually found on this computer; example services from an older app or beta report are not carried forward. If the Markdown file has the app or beta report title, it is replaced with the current report. An unrelated existing file is not overwritten: a file with the generated-section markers has only that section updated, while an unmarked file is left unchanged and the audit reports an error.

### Audit File Locations

Reports and `whitelist.csv` share the active audit folder. By default, the app uses its application directory when writable; otherwise, it uses `%LOCALAPPDATA%\WindowsServiceAudit`. Any output folder preference saved previously is still honored; the folder configuration menu is no longer available. If a configured folder becomes unavailable or unwritable, the audit reports the error and does not silently switch folders. Manual export destinations do not change the audit folder.

For an MSIX installation, Windows may redirect the logical fallback into the package's private `LocalCache\Local\WindowsServiceAudit` folder under `%LOCALAPPDATA%\Packages`.

### Understand the Results

`OK` means at least one whitelist term was found as a case-insensitive substring in the service's executable path or resolved executable target. `REVIEW` means neither path matched. These are path-text matches only; they do not establish whether a service is trusted, necessary, or safe to disable.

For each current `REVIEW` service, the report includes available local details such as state, startup setting, service account, executable and resolved paths, publisher/signature metadata, review signals, dependencies, and the Windows service description under **What this service does**. It also includes a Google Search link built from that service's display name and service name. The summary includes the current inventory and review counts. If there are no `REVIEW` services, the report says so and does not retain old service entries.

The table and CSV include service state, startup configuration, paths, review signals, process ID, signer metadata, and other available details. Some values may be blank when Windows restricts access or the information is not available. Embedded signer certificate information does not mean signature trust was evaluated.

The table's **Memory (MB)** column sorts numerically and is exported as `WorkingSetMB` in CSV. This working-set value is a process-level memory reading taken once after the selected interval. Services hosted together by `svchost.exe` share a process reading. The report does not include CPU usage.

### Edit the Whitelist

Choose **File > Edit whitelist** to add, change, or remove terms in the app. Enter one term per line and choose **Save**. A confirmation shows where the file was saved. The resulting `whitelist.csv` is stored in the active audit folder, with one `WhitelistTerm` column. The app reads the saved terms on the next audit. Matching is a case-insensitive substring search, so broad terms can match unrelated paths. Keep entries specific to the software or Windows components you intend to recognize.

The starter whitelist is only a convenience, not a recommended allowlist. You are responsible for reviewing its contents and any changes you make.

## Privacy and Safety

Audits and reports are generated locally. `Win-Srv-Eval-Report.md` includes a Google Search link for each `REVIEW` service, but the app does not contact Google automatically. Opening a link sends that service name to Google. Results are advisory: a service that matches the whitelist is not necessarily safe to disable, and a service marked `REVIEW` is not necessarily harmful or unnecessary. Stopping or disabling services in Windows Services can affect Windows or installed software. Confirm a service's purpose and the consequences with the relevant Windows feature or software provider before changing anything.

## Documentation

- [Frequently asked questions](docs/faq.md)
- [Developer guide: source, tests, and publishing](docs/development.md)

---

Developed by: [Richard Harris](https://github.com/richardharris84)
