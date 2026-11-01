# GitHub Actions Implementation Record

Originally recorded: October 30, 2026.

Current implementation verified: November 1, 2026 (UTC).

Repository: <https://github.com/richardharris84/WindowsAudit>  
Actions dashboard: <https://github.com/richardharris84/WindowsAudit/actions>

## Original User Prompt

> Scan this workspace and suggest a handful of GitHub Actions that you suggest we implement. Perhaps things like Codacy Security Scan, AI issue summary, or Greetings?
>
> https://github.com/richardharris84/WindowsAudit/actions

### Implementation Selection

The follow-up form response selected:

- Build & Test CI
- CodeQL Security Scan
- Release (installer + MSIX on tag)
- PSScriptAnalyzer lint
- Greetings
- AI issue summary (GitHub Models)
- Stale issues

The git-handling field contained the repository URL:
`https://github.com/richardharris84/WindowsAudit.git`.

Dependabot was not selected. The original release workflow built only the Inno
Setup installer; unsigned MSIX packaging has since been implemented and verified.

## What was implemented and pushed to `master` — current verified status

The following reflects inspected workflow files and completed runs on the
[Actions dashboard](https://github.com/richardharris84/WindowsAudit/actions).
Successful runs are evidence of those executions, not a guarantee of future runs.

| Workflow | Implemented behavior | Execution evidence |
|---|---|---|
| [CI](.github/workflows/ci.yml) | Windows .NET 10 Release build and tests on master pushes, PRs, and manual dispatch | ✅ [Passed on current master](https://github.com/richardharris84/WindowsAudit/actions/runs/41277028069); release validation now covers 20 tests |
| [CodeQL](.github/workflows/codeql.yml) | C# and GitHub Actions analysis on pushes/PRs and weekly schedule | ✅ [Passed on current master](https://github.com/richardharris84/WindowsAudit/actions/runs/41277028061); a successful scan does not establish zero alerts |
| [PSScriptAnalyzer](.github/workflows/psscriptanalyzer.yml) | PowerShell static analysis on relevant changes and manual dispatch; errors fail the job | ✅ [Passed](https://github.com/richardharris84/WindowsAudit/actions/runs/41254483876) |
| [Release](.github/workflows/release.yml) | Tests, self-contained x64 EXE installer, unsigned MSIX, metadata/content checks, checksums, artifacts, and draft release on a matching version tag | ✅ [v1.0.5.0 passed](https://github.com/richardharris84/WindowsAudit/actions/runs/41261445805); downloaded package hashes, versions, Store display name, and README verified |
| [Greetings](.github/workflows/greetings.yml) | First-interaction welcome comments for issues and PRs | ✅ [Issue-event run passed](https://github.com/richardharris84/WindowsAudit/actions/runs/41277568725); not every event posts a greeting |
| [AI Issue Summary](.github/workflows/ai-issue-summary.yml) | Copilot-powered triage comment when an issue is opened, using the configured secret and CLI default model | ✅ [Run passed](https://github.com/richardharris84/WindowsAudit/actions/runs/41277056169) and [actual automated comment](https://github.com/richardharris84/WindowsAudit/issues/10#issuecomment-6553924018); [second comment verified](https://github.com/richardharris84/WindowsAudit/issues/11#issuecomment-6553974031) |
| [Stale](.github/workflows/stale.yml) | Daily/manual processing; stale after 60 days, close after 14 more, with exemptions | ✅ [Run passed](https://github.com/richardharris84/WindowsAudit/actions/runs/41174124159); ⏳ actual age-based marking/closure has not been verified |

The [v1.0.5.0 release](https://github.com/richardharris84/WindowsAudit/releases/tag/v1.0.5.0)
was subsequently published by the maintainer. The workflow itself still creates
drafts. Its assets are `WindowsAudit-Setup.exe`,
`WindowsAudit_1.0.5.0_x64.msix`, and `SHA256SUMS.txt`.

## Additional Implementation Details

These details describe the current workflow files and their operational limitations.

### 1. Build & Test CI

File: [ci.yml](.github/workflows/ci.yml).

- Runs for pushes to `master`, pull requests targeting `master`, and manual
  dispatch.
- Uses `windows-latest` and .NET `10.0.x`.
- Restores, builds in Release configuration, and tests
  [WindowsServiceAudit.slnx](WindowsServiceAudit.slnx).
- Uses `actions/checkout@v7` and `actions/setup-dotnet@v6`.
- Grants only `contents: read`.
- Does not configure branch protection or require CI to pass before merging.
  Those are separate repository settings.

### 2. CodeQL

File: [codeql.yml](.github/workflows/codeql.yml).

- Runs for pushes and pull requests on `master`, plus Mondays at 06:30 UTC.
- Analyzes C# using a manual .NET Release build on `windows-latest`.
- Separately analyzes GitHub Actions workflows on `ubuntu-latest` with no build.
- Uses `github/codeql-action/init@v4` and
  `github/codeql-action/analyze@v4`.
- Grants `security-events: write`, `packages: read`, `actions: read`, and
  `contents: read`.
- Findings appear in the repository's
  [code scanning alerts](https://github.com/richardharris84/WindowsAudit/security/code-scanning).
- CodeQL was implemented instead of Codacy. This does not imply the two products
  have identical coverage or features.

### 3. Release

File: [release.yml](.github/workflows/release.yml).

- Runs when a tag matching `v*` is pushed, or by manual dispatch.
- Runs tests before invoking [Build-Installer.ps1](installer/Build-Installer.ps1)
  and [Build-MSIX.ps1](packaging/Build-MSIX.ps1).
- Relies on Inno Setup being available on the Windows runner.
- Builds a self-contained Windows x64 installer and unsigned Store MSIX, and
  generates SHA-256 checksums for both.
- Requires matching Store-compatible application and manifest versions and,
  on tag runs, a matching `v<version>` tag.
- Verifies that the MSIX display name is exactly `Windows Audit`, matching the
  reserved Store name, without a version suffix.
- Checks that the completed MSIX includes the Getting Started README and that
  its text matches the repository source.
- Packaging scripts use unique system temporary directories for installer
  compilation and MSIX staging to avoid workspace synchronization locks.
- Published README, documentation, and whitelist assets explicitly remain loose
  files even in single-file builds.
- Uploads `WindowsAudit-Setup.exe`, the versioned MSIX, and `SHA256SUMS.txt` as a
  workflow artifact named `WindowsAudit-Release`.
- On a `v*` tag reference, also creates a **draft** GitHub Release with generated
  release notes and all three files attached.
- A manual run on a branch builds and uploads the artifact but does **not**
  create a GitHub Release.
- Uses `actions/upload-artifact@v7` and `softprops/action-gh-release@v3`.
- Grants `contents: write`.

**Limitations:** The workflow does not sign either package or derive the
application version from the tag. The tag must match the version already set in
the project. The unsigned MSIX is intended for Store submission, not direct
installation without appropriate signing. Checksums detect file changes but do
not provide a trusted publisher signature.

**MSIX clarification:** Building an unsigned MSIX and signing it for direct
installation are separate operations. A certificate is needed for signed direct
installation, not a requirement to generate an
unsigned package. Unsigned MSIX automation is now configured; no signing secret
is required for that build.

### 4. PSScriptAnalyzer

File: [psscriptanalyzer.yml](.github/workflows/psscriptanalyzer.yml).

- Runs on pushes and pull requests on `master` when PowerShell scripts or this
  workflow change; also supports manual dispatch.
- Uses PowerShell on `ubuntu-latest`.
- Installs PSScriptAnalyzer from the PowerShell Gallery during each run.
- Recursively scans the checked-out workspace for Warning and Error diagnostics.
- Prints warnings but fails only for Error diagnostics.
- Grants only `contents: read`.
- Performs static analysis; it does not execute Windows-specific scripts or
  prove that installer, packaging, branding, or trailer generation works.

### 5. Greetings

File: [greetings.yml](.github/workflows/greetings.yml).

- Uses `actions/first-interaction@v3` for issue and `pull_request_target` events.
- Posts project-specific welcome messages for first-time issue authors and PR
  contributors.
- Requests Windows version, installation method, and reproduction steps for
  issues; reminds PR contributors to build and test.
- Grants `issues: write` and `pull-requests: write`.
- Does not check out or execute pull request code.

**Privacy:** The issue greeting suggests attaching a report section. Contributors
should redact sensitive service names, paths, account details, and other local
system information before posting to this public repository.

### 6. AI Issue Summary

File: [ai-issue-summary.yml](.github/workflows/ai-issue-summary.yml).

- Runs only when an issue is opened; edits do not refresh its summary.
- Installs the Copilot CLI and uses `actions/ai-inference@v3` with a Windows
  Audit-specific system prompt. No tool permissions are granted to the CLI.
- Explicitly sets `model: ''` so the CLI chooses its default rather than the
  action's unavailable `gpt-4.1` default.
- Requests three short bullets covering the report, likely affected area, and
  missing information, with a requested limit of 120 words.
- Posts the response using `gh issue comment`, with a notice that the generated
  summary may contain mistakes.
- Grants `contents: read` and `issues: write`; the built-in GitHub token is used
  only to post comments.
- Requires a repository Actions secret named `COPILOT_PAT`: a personal
  fine-grained token with the **Copilot Requests** account permission, owned by
  an account with an active Copilot subscription. Classic PATs and the built-in
  `GITHUB_TOKEN` are not supported for Copilot inference.
- If `COPILOT_PAT` is absent, emits a warning and skips inference and commenting.
  Empty model responses are not posted.

**Operational notes:** [GitHub Models was retired on July 30, 2026](https://docs.github.com/en/github-models),
including its inference API. The former `actions/ai-inference@v1` configuration
failed on the retired endpoint with `Cannot use 'in' operator to search for
'choices' in OK`; it has been replaced with Copilot inference. Copilot access,
repository or organization policy, and applicable quotas can affect execution.
The word limit is a prompt
instruction, not a programmatically enforced limit. Issue title and body are
sent to model inference; do not include confidential data or credentials in
issues.

**Verified follow-up:** Issue #8 initially produced a successful run with a
missing-secret warning and no comment. After the secret was configured, retries
failed because `gpt-4.1` was unavailable. [PR #9](https://github.com/richardharris84/WindowsAudit/pull/9)
merged the empty-model fix into master at
[`2de8bd4`](https://github.com/richardharris84/WindowsAudit/commit/2de8bd46b373e2ef40552350b66e516a009f36d0).
New issues #10 and #11 received automated comments, proving inference and posting
end-to-end. Issue #8 received a clearly labeled manual backfill, not an automated
comment. Rerunning its original run uses its original workflow, not the fix.

### 7. Stale Issues and Pull Requests

File: [stale.yml](.github/workflows/stale.yml).

- Uses `actions/stale@v11`.
- Runs daily at 05:45 UTC and supports manual dispatch.
- Marks issues and PRs stale after 60 days of inactivity, then closes them after
  another 14 inactive days.
- Uses the `stale` label and posts explanatory comments.
- Exempts issues labeled `pinned`, `security`, or `enhancement`.
- Exempts PRs labeled `pinned` or `security`.
- Limits processing to 50 operations per run.
- Grants `issues: write` and `pull-requests: write`.

Scheduled workflows run from the default branch and may be delayed by GitHub.
GitHub can disable scheduled workflows in public repositories after prolonged
repository inactivity.

## Validation and Implementation History

The original implementation was pushed in two commits:

- [`d7dd159`](https://github.com/richardharris84/WindowsAudit/commit/d7dd159):
  added the seven workflows.
- [`94cc9ae`](https://github.com/richardharris84/WindowsAudit/commit/94cc9ae):
  restricted token permissions in CI and PSScriptAnalyzer.

Historical validation recorded in the implementation conversation:

- Local .NET SDK: `10.0.403`; all 17 tests passed.
- All seven YAML files parsed successfully using `js-yaml`.
- Local PSScriptAnalyzer: 0 errors and 1 `PSUseApprovedVerbs` warning.
- GitHub CI, CodeQL, and PSScriptAnalyzer runs passed.
- CodeQL initially reported two `actions/missing-workflow-permissions` alerts;
  both were resolved by the second commit, leaving 0 open alerts at that time.

During the original validation, **only CI, CodeQL, and PSScriptAnalyzer were
execution-verified.** Release,
Greetings, AI Issue Summary, and Stale still needed their respective triggers
and end-to-end validation. These are historical results, not a live status
report or a guarantee of future runs.

Subsequent merged changes added and verified both release packages, Store-name
and README checks, local packaging reliability fixes, and working Copilot issue
summaries. See the current status table above for execution evidence. In
particular, [PR #6](https://github.com/richardharris84/WindowsAudit/pull/6)
contains the packaging corrections and
[PR #9](https://github.com/richardharris84/WindowsAudit/pull/9) fixes model selection.
The [v1.0.5.0 tag](https://github.com/richardharris84/WindowsAudit/releases/tag/v1.0.5.0)
includes the merged packaging and dialog fixes; the later AI model fix is on
master and does not change those already-published packages.

## Maintenance and Remaining Work

- Preserve Release's verified tag path, metadata/content checks, and draft default.
- Keep `COPILOT_PAT` valid with Copilot access; renew it before expiration and
  monitor inference failures or quotas. Do not interpret a missing-secret
  warning as successful comment posting.
- Validate Stale's actual age-based marking/closure and exemptions before relying
  on automatic maintenance of long-running work.
- Add trusted package signing only if separately required; unsigned Store MSIX
  generation is already implemented.
- Consider Dependabot for NuGet and GitHub Actions updates; it was not added.
- Actions currently use major-version tags, not immutable commit SHA pins.
- Review draft release assets and notes before publishing.
- Review inactivity thresholds and exemption labels before relying on automatic
  closure for long-running work.
