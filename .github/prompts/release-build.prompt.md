---
name: release-build
description: "Create a full Windows Audit EXE and MSIX build, update matching versions, and create a verified GitHub Release using the Release action."
argument-hint: "Optional version, release changes, and draft or public publication"
agent: agent
---

Create a new full Windows Audit build and GitHub Release. Execute the workflow
end-to-end rather than only describing commands. Use any version, release changes,
or publication preference supplied with this prompt.

## Inspect and choose the version

- Read the current Git status, branch/upstream, remote, recent commits, tags, and
  GitHub releases (including drafts and prereleases). Use `gh` for GitHub operations.
- Preserve unrelated uncommitted work. Do not stage it or release it accidentally.
  Ask before including existing uncommitted changes or if the release branch is
  ambiguous. Do not reset, force-push, overwrite tags, or discard anyone's work.
- Inspect the [Release workflow](../workflows/release.yml),
  [app project](../../src/WindowsServiceAudit.App/WindowsServiceAudit.App.csproj),
  [MSIX manifest](../../packaging/AppxManifest.xml),
  [installer script](../../installer/Build-Installer.ps1),
  [installer definition](../../installer/WindowsAudit.iss),
  [MSIX build script](../../packaging/Build-MSIX.ps1), and
  [developer guide](../../docs/development.md).
- Use one version for the application, EXE installer, and MSIX. It must have four
  numeric components, each in the range 0-65535, with the final component zero
  for Microsoft Store submission.
- If no version is supplied, choose the next unused patch version ending in `.0`,
  greater than current project/package versions and existing release versions,
  tags, and versioned build artifacts. Explain the selected version.
- Reject a supplied version that is invalid, already used, or not newer; ask for
  an alternative rather than silently substituting one.
- Default to a **draft** GitHub Release. Publish publicly only when explicitly
  requested. Do not change the workflow's default draft behavior just to publish
  one release.

## Update and validate

- Implement only the requested release changes; do not repeat previous UI changes
  unless requested. Add focused regression tests for behavior changes.
- Set the app project's `Version` and MSIX manifest identity version to the
  selected version. Set the MSIX package display name to
  `Windows Audit <version>` while preserving its Store identity and publisher.
- Ensure Add/Remove Programs shows `Windows Audit <version>` and publisher
  `Built by Richard Harris`. Keep the installer version/display name derived from
  the executable, and keep the MSIX build script's default derived from the app
  project. Preserve existing installation paths, shortcuts, and upgrade identity.
- Update directly related version/build documentation, including current
  reconstruction instructions in [Copilot-Prompts.md](../../Copilot-Prompts.md).
  Do not rewrite historical release notes or archived examples as current releases.
- Detect the repository's test platform and use its existing Release test command.
  Run focused tests while developing and the solution tests before release.
- Build the complete self-contained Windows x64 EXE installer and unsigned Store
  MSIX locally using the existing scripts:

  ```powershell
  .\installer\Build-Installer.ps1
  pwsh -STA -File .\packaging\Build-MSIX.ps1
  ```

- Verify the published executable's file version, the installer configuration's
  versioned installed display name/publisher, and the built MSIX's embedded
  `AppxManifest.xml` version/display name. Clearly distinguish metadata validation
  from an actual installation test; do not install packages without permission.
- Check patch whitespace and relevant script/workflow validation using existing
  tooling. Do not install or replace tools speculatively. If local packaging tools
  are missing, report that limitation and use the Release action to build and
  verify the packages; do not claim a local build passed.

## Create the release through GitHub Actions

- Ensure the Release action still tests, builds both packages, validates matching
  versions and tag, creates checksums, and attaches the EXE installer, versioned
  MSIX, and `SHA256SUMS.txt`. Do not bypass it with a manual binary upload.
- Review the diff. Commit only the intended release changes using explicit paths.
  Include this commit trailer:

  ```text
  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
  ```

- Push the release commit to the confirmed release branch. If protection requires
  a pull request, use that path and wait for the exact changes to be merged before
  tagging; do not bypass branch protection.
- Create a new annotated `v<version>` tag on the exact release commit and push
  that tag to trigger the Release action. Never move or replace an existing tag.
  A manual dispatch on a branch alone does not create a GitHub Release.
- Find the run for that exact tag and commit, wait for completion, and inspect
  failed-step logs if it fails. Resolve in-scope failures without hiding errors or
  retagging silently. Do not declare success merely because the run started.
- Verify the resulting release's tag, draft/public state, and complete asset list:
  `WindowsAudit-Setup.exe`, `WindowsAudit_<version>_x64.msix`, and `SHA256SUMS.txt`.
- Download the action-built release assets into a new version-specific folder
  under `artifacts`, without overwriting existing files. Check both package hashes
  against `SHA256SUMS.txt`. Verify the downloaded MSIX's embedded version/display
  name as well. Keep these deliverables available locally.
- If public publication was explicitly requested, publish the verified draft
  using `gh release edit` and verify that it is no longer a draft.
- Keep packages unsigned unless signing was explicitly requested and credentials
  are provided through an approved secure mechanism. Never commit signing secrets
  or ask public users to trust a development certificate. Explain that the
  unsigned MSIX is for Store submission, not ordinary direct installation.

## Final response

Report the selected shared version, requested changes, tests/build results,
Release action result, release link and draft/public state, and links to the
downloaded packages. State any outstanding limitations explicitly. Do not claim
the release is complete until the action and asset/checksum verification succeed.
