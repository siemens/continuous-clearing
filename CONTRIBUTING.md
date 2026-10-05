# Contributing

When contributing to this repository, first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change. Make sure a pull request is made after every changes before merging to master.

Note we have a code of conduct, follow it in all your interactions with the project

We welcome contributions in several forms, e.g.

- Documenting
- Testing
- Coding


## Getting Started
1. Installation process - 
    Install the following basic prerequisites:
    * Git (any recent version will do) -
      Clone the repository from <CA_Project_RepoLink>

2. Software dependencies -
     Visual Studio 2026, .NET 10
	
## Pull Request Process

1. Ensure any installed or build dependencies are removed before the end of the layer when doing a build.
2. Update the README.md and Changelog.md with details of changes to the code, this includes new environment variables, exposed ports, useful file locations and container parameters.
3. Ensure the PR description clearly describes the problem and solution.

## Running the tests
The methods in all the the 3 executables are tested through Unit Tests (UTs). 
Workflows are validated through Integration Tests (IT).
After making any code changes ensure that proper UT and IT test cases are also added.

The simplest way to run tests:
```bash
- cd src
- dotnet test --no-build -c release
 ```




## Release Process

Releases are automated via the `Build & Release` GitHub Actions workflow
(`.github/workflows/build-and-release.yml`). Version numbers are computed
automatically by **GitVersion** based on branch/tag naming, using the rules
defined in [`GitVersion.yml`](GitVersion.yml).

### Versioning is fully automatic - no manual steps or stale versions

None of the `.csproj` files contain a hardcoded `<Version>` element anymore,
and `src/Directory.Build.props` only defines a harmless `0.0.0-dev` fallback
for local/dev builds. Contributors never need to remember to bump a version
anywhere, and there is no checked-in version number that can go stale.

The real version is computed live by GitVersion in the `version` job of the
CI workflow and passed into `dotnet build`/`dotnet pack`/the Docker tag via
MSBuild properties (`-p:Version`, `-p:AssemblyVersion`, `-p:FileVersion`,
`-p:InformationalVersion`), so every CI-built DLL, NuGet package, and Docker
image always carries the correct version derived from Git history according
to the rules in [`GitVersion.yml`](GitVersion.yml).

### Versioning rules (GitVersion.yml)

| Branch / Tag pattern         | GitVersion tag | Example version   | Release type       |
|-------------------------------|----------------|--------------------|----------------------|
| `main` / `master`             | (none)         | `2.5.0`            | Stable               |
| `beta/*` or `beta-*`          | `beta`         | `2.5.0-beta.1`     | Pre-release (beta)   |
| `release/*` or `release-*`    | `rc`           | `2.5.0-rc.1`       | Pre-release (RC)     |

### Bumping the version manually

`master` and `beta` are configured with `increment: None` in
[`GitVersion.yml`](GitVersion.yml), so **ordinary commits never change the
version** - this avoids every routine commit silently bumping the patch
version and causing version drift/collisions between beta and stable builds.

**To cut a real release, no commit message convention is required.**
Trigger the workflow manually and pick the bump type:

1. Go to **Actions -> Build & Release -> Run workflow**.
2. Set `version-bump` to `patch`, `minor`, or `major` (leave as `none` for a
   normal build/pre-release with no version change).
3. Optionally set `ref` to the branch/tag you want to release from.
4. Run the workflow - GitVersion will compute the new version using the
   selected bump on top of the last tag, build/pack/tag Docker with that
   version, and create a draft release.

Alternative ways to bump (only needed in special cases):

1. **`next-version` in `GitVersion.yml`** - Set a floor version so all
   subsequent builds compute from at least that version:
   ```yaml
   next-version: 3.0.0
   ```
   Useful for a deliberate, one-time major/minor bump; revert or update
   this value after the release if no longer needed.

3. **Base tag on `master`** - Push a tag (e.g. `v3.0.0`) on `master` to set
   a new baseline that GitVersion will increment from for subsequent builds.

### How releases are triggered

The workflow runs on:
- Push to `main` (stable release)
- Push to `beta/**` or `release/**` branches (pre-release)
- Manual `workflow_dispatch` with an optional `ref` input (branch or tag),
  allowing a maintainer to trigger a build/release from any approved ref
  without depending on Power Branch details - useful for hotfixes.

> Note: Pushing a git tag does **not** trigger this workflow. Tags are only
> created as a result of the `release` job (via `actions/create-release`)
> once a release is published.

> **Important:** Only create and push a `release/*` or `beta/*` branch when
> you actually intend to cut a pre-release. Because pushing these branches
> immediately triggers a tag and pre-release, avoid using these prefixes
> for regular feature/work-in-progress branches - use them exclusively for
> preparing a beta or RC pre-release.

### Pre-release vs. stable behavior

- Every release created by this workflow - stable or pre-release - is
  always created as a **draft** (`--draft` is always passed to
  `gh release create`). A release is never auto-published, so you can
  review the generated notes/assets and decide when to make it public;
  this also means it never silently overwrites the "Latest release"
  pointer before you are ready.
- If GitVersion's `preReleaseTag` output is non-empty (beta/rc), the draft
  release is additionally marked `prerelease: true`, clearly flagging it
  as a pre-release once published.
- Stable releases (no pre-release tag) are created as a plain draft
  (`prerelease: false`); publishing it from the **Releases** page is what
  makes it the new "Latest release".
- Before creating a release, the workflow checks whether the computed tag
  (`vX.Y.Z`) already exists and skips release creation if so, to avoid
  duplicate/escalating releases.
- **After a release is reviewed and ready to go live, publish the draft
  manually from the GitHub Releases page** (or via `gh release edit <tag>
  --draft=false`). This manual step is required for every release.

### Creating a hotfix release

1. Use **Actions -> Build & Release -> Run workflow** and supply the approved
   branch or tag name in the `ref` input.

No dependency on Power Branch details is required.

### Contributor checklist for release-related changes

If your change affects branch naming, versioning, or the release workflow:
- Update [`GitVersion.yml`](GitVersion.yml) and this section together.
- Update `.github/workflows/build-and-release.yml` and document any new
  triggers, jobs, or release-type behavior here.
- Verify the change with a test run (PR or manual `workflow_dispatch`)
  before merging to `main`.
