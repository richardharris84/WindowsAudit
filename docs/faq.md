# Frequently Asked Questions

## Does the app change Windows services?

No. It reads service and file metadata and writes reports to its configured audit folder. By default, this is its application directory, or `%LOCALAPPDATA%\WindowsServiceAudit` if the application directory is not writable. Any output folder preference saved previously is still honored. The app does not start, stop, disable, or reconfigure services.

## Does `REVIEW` mean a service is dangerous?

No. `REVIEW` only means the executable path and resolved target did not match a whitelist term. A match marked `OK` also does not prove that the service is trustworthy, essential, or safe to disable. See [Understanding the results](../README.md#understand-the-results).

## Why are some values blank or unavailable?

Windows may deny access to optional registry or process metadata, or the service may not expose that information. The audit continues when optional registry metadata is inaccessible. Blank values are not proof that a service is suspicious.

## Does the app send my service inventory anywhere?

Inventory and reports are generated locally. `Win-Srv-Eval-Report.md` includes a Google Search link based on each `REVIEW` service's Windows service name. The app does not contact Google automatically; opening one of those links sends the service name to Google, which may display an AI Overview and search results.

## Where are the reports saved?

The configured audit directory receives `Win-Srv-Eval-Results.csv`, `Win-Srv-Eval-Report.md`, and `whitelist.csv`. By default, this is the application directory when writable, or `%LOCALAPPDATA%\WindowsServiceAudit` otherwise. Any output folder preference saved previously is still honored. Manual CSV and DOCX exports let you choose a destination without changing the audit directory. Use **File > Edit whitelist** to change whitelist terms in the app.

## Why is there no CPU percentage?

The report records a point-in-time process working-set reading after the selected wait interval. It does not collect CPU usage.

[← Back to the README](../README.md)