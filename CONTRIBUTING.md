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

### Versioning rules (GitVersion.yml)

| Branch / Tag pattern         | GitVersion tag | Example version   | Release type       |
|-------------------------------|----------------|--------------------|----------------------|
| `main` / `master`             | (none)         | `2.5.0`            | Stable               |
| `beta/*` or `beta-*`          | `beta`         | `2.5.0-beta.1`     | Pre-release (beta)   |
| `release/*` or `release-*`    | `rc`           | `2.5.0-rc.1`       | Pre-release (RC)     |

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

- If GitVersion's `preReleaseTag` output is non-empty (beta/rc), the release
  is created with `prerelease: true` in GitHub - clearly marked and does
  **not** replace the "Latest release" pointer.
- Stable releases (no pre-release tag) are created as drafts and require a
  maintainer to manually publish them from the **Releases** page.
- Before creating a release, the workflow checks whether the computed tag
  (`vX.Y.Z`) already exists and skips release creation if so, to avoid
  duplicate/escalating releases.

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
