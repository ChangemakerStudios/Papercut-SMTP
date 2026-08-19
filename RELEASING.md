# Release Procedure

This project follows [Gitflow](https://nvie.com/posts/a-successful-git-branching-model/) and uses [GitVersion](https://gitversion.net/) (ContinuousDelivery mode) for semantic versioning, with [Velopack](https://velopack.io/) for desktop installer packaging. The CI/CD pipeline runs via GitHub Actions and handles building, packaging, and publishing automatically based on branch.

## Branch Overview

| Branch      | Purpose                                   | Channel   | GitHub Release | Docker Tag                | WinGet |
|-------------|-------------------------------------------|-----------|----------------|---------------------------|--------|
| `master`    | Production releases only                  | `-stable` | Stable release | `latest`, `X.Y.Z`, `X.Y` | Yes    |
| `develop`   | Integration branch for the next release   | `-dev`    | Pre-release    | `dev`, full semver        | No     |
| `feature/*` | Feature branches off `develop`            | `-alpha`  | —              | —                         | No     |
| `release/*` | Release stabilization branches off `develop` | `-alpha` | —            | —                         | No     |
| `hotfix/*`  | Critical fixes off `master`               | `-alpha`  | —              | —                         | No     |

## Versioning

GitVersion derives the version automatically:

- On a `release/X.Y.Z` branch, **the version comes from the branch name** — this is the canonical way to choose the next version.
- On `develop`, the version increments **Patch** per commit by default. A commit message containing `+semver: minor` or `+semver: major` forces a larger bump.
- Run `dotnet gitversion` locally at any time to check the computed version.

## Releasing a Stable Version (Gitflow)

### 1. Ensure `develop` is ready

- All feature branches for the release are merged into `develop`.
- CI is green on `develop` (tests + packaging).

### 2. Create the release branch

```bash
git checkout develop
git pull origin develop
git checkout -b release/X.Y.Z
```

### 3. Finalize the release on the branch

Release-only changes go here:

- **Update [ReleaseNotesCurrent.md](ReleaseNotesCurrent.md)** with the new version header and changes (this file is embedded in the installers).
  - Gather changes since the last release:
    ```bash
    gh pr list --state merged --search "merged:>YYYY-MM-DD" --limit 100
    git log --oneline X.Y.Z-previous..develop
    ```
  - Follow the established format — reference issue/PR numbers with links and **thank contributors and reporters** by GitHub username:
    ```markdown
    # Release Notes

    ## Papercut SMTP vX.Y.Z [YYYY-MM-DD]

    ### New Features
    - **Feature Name** - Description. Fixes [#123](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/123) (Thanks, [username](https://github.com/username)!)

    ### Improvements
    - **Area** - What was improved

    ### Bug Fixes
    - **Area** - What was fixed. Fixes [#456](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/456)

    ### Contributors
    Special thanks to [user1](https://github.com/user1) for ...!
    ```
- **Prepend the same section to [ReleaseNotes.md](ReleaseNotes.md)** (cumulative history).
- **Smoke test locally**:
  ```bash
  dotnet build Papercut.sln --configuration Release
  dotnet test Papercut.sln --configuration Release
  ```
- Optionally push the branch — CI will validate the build (`-alpha` channel, nothing is published).

### 4. Merge into `master` and tag

```bash
git checkout master
git pull origin master
git merge --no-ff release/X.Y.Z
git tag X.Y.Z
git push origin master --tags
```

The `master` push triggers [build.yml](.github/workflows/build.yml), which will:

- Run GitVersion (the computed version matches the tag)
- Run all tests
- Build UI installers for **x64**, **x86**, and **ARM64** via Velopack
- Build Service packages (self-contained ZIPs) for all three architectures
- Generate WinGet manifests
- Publish the **GitHub Release** for the `X.Y.Z` tag with all artifacts
- Build and push **Docker images** to Docker Hub (`latest`, version tags)

### 5. Merge the release branch back into `develop`

```bash
git checkout develop
git pull origin develop
git merge --no-ff release/X.Y.Z
git push origin develop
```

### 6. Clean up

```bash
git branch -d release/X.Y.Z
git push origin --delete release/X.Y.Z   # if the branch was pushed
```

### 7. Post-Release Verification

- [ ] GitHub Release exists for tag `X.Y.Z` with all expected artifacts:
  - `PapercutSMTP-*-win-x64-stable-Setup.exe`
  - `PapercutSMTP-*-win-x86-stable-Setup.exe`
  - `PapercutSMTP-*-win-arm64-stable-Setup.exe`
  - `Papercut.Smtp.Service.*-win-x64.zip`
  - `Papercut.Smtp.Service.*-win-x86.zip`
  - `Papercut.Smtp.Service.*-win-arm64.zip`
  - WinGet YAML manifests
- [ ] Docker Hub image updated: `changemakerstudiosus/papercut-smtp:latest`
- [ ] Download and run the installer — verify the app launches and receives test emails
- [ ] WinGet PR created by [winget-publish.yml](.github/workflows/winget-publish.yml) (or submit manually — see [installation/winget/README.md](installation/winget/README.md))

## Releasing a Pre-Release (from `develop`)

Pushing to `develop` automatically creates a **pre-release** on GitHub with `-dev` channel artifacts and pushes a `dev`-tagged Docker image. No manual steps required beyond merging your feature branches.

## Hotfix Releases (Gitflow)

For critical fixes to a released version:

```bash
git checkout master
git pull origin master
git checkout -b hotfix/X.Y.Z+1
# make the fix, update ReleaseNotesCurrent.md / ReleaseNotes.md, commit

git checkout master
git merge --no-ff hotfix/X.Y.Z+1
git tag X.Y.Z+1
git push origin master --tags

git checkout develop
git merge --no-ff hotfix/X.Y.Z+1
git push origin develop

git branch -d hotfix/X.Y.Z+1
```

The `master` push triggers the full stable release pipeline, exactly as in a normal release.

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

- **Version not what you expected?** GitVersion derives version from git history/tags/branch name. Run `dotnet gitversion` locally to check. On a `release/X.Y.Z` branch the branch name wins.
- **Release came out as `X.Y.Z-N` (e.g. `7.8.0-33`)?** `N` is the number of commits since the last tag — the pipeline ran before the `X.Y.Z` tag was pushed. The artifacts are also internally stamped with the wrong version, so don't just rename the release: delete the draft, ensure the tag is on the `master` HEAD commit, and re-run the workflow (`gh run rerun <run-id>`). **Always push the tag together with the branch** (`git push origin master --tags`) so CI sees it.
- **Deploy skipped?** `DeployReleases` only runs on `master`/`develop` in GitHub Actions with `GITHUB_TOKEN` present.
- **WinGet PR not created?** Check that `WINGET_PUBLISH_TOKEN` secret is configured. See [installation/winget/README.md](installation/winget/README.md).
- **Docker push failed?** Verify `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets are set.
