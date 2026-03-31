# Release Procedure

This document describes how to release a new version of Papercut SMTP.

## Overview

Papercut uses [GitVersion](https://gitversion.net/) (ContinuousDelivery mode) for semantic versioning and [Velopack](https://velopack.io/) for desktop installer packaging. The CI/CD pipeline runs via GitHub Actions and handles building, packaging, and publishing automatically based on branch.

| Branch    | Channel     | GitHub Release | Docker Tag                  | WinGet |
|-----------|-------------|----------------|-----------------------------|--------|
| `master`  | `-stable`   | Stable release | `latest`, `X.Y.Z`, `X.Y`   | Yes    |
| `develop` | `-dev`      | Pre-release    | `dev`, full semver          | No     |
| Other     | `-alpha`    | —              | —                           | No     |

## Pre-Release Checklist

1. **Verify `develop` is green** — Ensure the latest CI build on `develop` passes (tests, packaging).

2. **Gather changes since last release**
   - Review all PRs merged since the last release tag:
     ```bash
     # List PRs merged since last release
     gh pr list --state merged --search "merged:>YYYY-MM-DD" --limit 100
     # Or compare commits between last tag and develop
     git log --oneline --merges vX.Y.Z..develop
     ```
   - Review commits for any changes not covered by PRs:
     ```bash
     git log --oneline vX.Y.Z..develop
     ```
   - Note the GitHub usernames of PR authors and issue reporters for attribution.

3. **Update release notes**
   - Edit [ReleaseNotesCurrent.md](ReleaseNotesCurrent.md) with the new version header and changes.
   - Follow the established format — reference issue/PR numbers with links and **thank contributors** by GitHub username. Example:
     ```markdown
     # Release Notes

     ## Papercut SMTP vX.Y.Z [YYYY-MM-DD]

     ### New Features
     - **Feature Name** - Description of the feature. Fixes [#123](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/123) (Thanks, [username](https://github.com/username)!)
       - Sub-detail about the feature

     ### Improvements
     - **Area** - What was improved

     ### Bug Fixes
     - **Area** - What was fixed. Fixes [#456](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/456)

     ### Contributors
     Special thanks to [user1](https://github.com/user1) for ... and [user2](https://github.com/user2) for ...!
     ```
   - Prepend the same content to [ReleaseNotes.md](ReleaseNotes.md) (cumulative history).

3. **Smoke test locally** (optional but recommended)
   ```powershell
   dotnet restore Papercut.sln
   dotnet build Papercut.sln --configuration Release
   dotnet test Papercut.sln --configuration Release
   ```

## Releasing a Stable Version

### 1. Merge `develop` → `master`

```bash
git checkout master
git pull origin master
git merge develop
git push origin master
```

Or create a PR from `develop` → `master` and merge it.

### 2. CI Builds and Publishes Automatically

Once pushed to `master`, GitHub Actions ([build.yml](.github/workflows/build.yml)) will:

- Run GitVersion to determine the version
- Run all tests
- Build UI installers for **x64**, **x86**, and **ARM64** via Velopack
- Build Service packages (self-contained ZIPs) for all three architectures
- Generate WinGet manifests
- Create/update a **GitHub Release** (stable) with all artifacts
- Build and push **Docker images** to Docker Hub (`latest`, version tags)

### 3. WinGet Publishing (Automatic)

After the GitHub Release is published, [winget-publish.yml](.github/workflows/winget-publish.yml) triggers automatically:

- Downloads WinGet manifest YAML files from the release
- Validates them with `winget validate`
- If `WINGET_PUBLISH_TOKEN` is configured: creates a PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)
- If not: logs manual submission instructions

See [installation/winget/README.md](installation/winget/README.md) for WinGet setup details.

### 4. Post-Release Verification

- [ ] GitHub Release exists with correct version and all expected artifacts:
  - `PapercutSMTP-*-win-x64-stable-Setup.exe`
  - `PapercutSMTP-*-win-x86-stable-Setup.exe`
  - `PapercutSMTP-*-win-arm64-stable-Setup.exe`
  - `Papercut.Smtp.Service.*-win-x64.zip`
  - `Papercut.Smtp.Service.*-win-x86.zip`
  - `Papercut.Smtp.Service.*-win-arm64.zip`
  - WinGet YAML manifests
- [ ] Docker Hub image updated: `changemakerstudiosus/papercut-smtp:latest`
- [ ] Download and run the installer — verify the app launches and receives test emails
- [ ] WinGet PR created (or submit manually if needed)

### 5. Sync `master` back to `develop`

```bash
git checkout develop
git pull origin develop
git merge master
git push origin develop
```

## Releasing a Pre-Release (from `develop`)

Pushing to `develop` automatically creates a **pre-release** on GitHub with `-dev` channel artifacts and pushes a `dev`-tagged Docker image. No manual steps required beyond merging your feature branches.

## Hotfix Releases

1. Create a `hotfix/*` branch from `master`
2. Apply the fix, update release notes
3. Merge into both `master` and `develop`
4. The `master` push triggers the full stable release pipeline

## Artifacts Produced

| Artifact | Description |
|----------|-------------|
| `PapercutSMTP-*-Setup.exe` | Velopack desktop installer (per arch/channel) |
| `Papercut.Smtp.Service.*.zip` | Self-contained Windows Service (per arch) |
| Docker image | Linux container (`changemakerstudiosus/papercut-smtp`) |
| WinGet manifests | YAML files for Windows Package Manager submission |

## Key Files

| File | Purpose |
|------|---------|
| [build.cake](build.cake) | Cake build script — all build/package/deploy tasks |
| [build.ps1](build.ps1) | Bootstrap script (installs Cake + vpk tools) |
| [build/Velopack.cake](build/Velopack.cake) | Velopack pack and upload helpers |
| [build/WinGet.cake](build/WinGet.cake) | WinGet manifest generation |
| [build/ReleaseNotes.cake](build/ReleaseNotes.cake) | Release notes parsing |
| [.github/workflows/build.yml](.github/workflows/build.yml) | CI/CD pipeline |
| [.github/workflows/winget-publish.yml](.github/workflows/winget-publish.yml) | WinGet auto-publish |
| [GitVersion.yml](GitVersion.yml) | Versioning configuration |
| [ReleaseNotesCurrent.md](ReleaseNotesCurrent.md) | Release notes for current version (embedded in installers) |
| [ReleaseNotes.md](ReleaseNotes.md) | Cumulative release history |

## Troubleshooting

- **Version not what you expected?** GitVersion derives version from git history/tags. Run `dotnet gitversion` locally to check.
- **Deploy skipped?** `DeployReleases` only runs on `master`/`develop` in GitHub Actions with `GITHUB_TOKEN` present.
- **WinGet PR not created?** Check that `WINGET_PUBLISH_TOKEN` secret is configured. See [installation/winget/README.md](installation/winget/README.md).
- **Docker push failed?** Verify `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets are set.
