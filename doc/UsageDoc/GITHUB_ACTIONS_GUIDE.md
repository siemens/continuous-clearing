# Siemens Continuous Clearing Tool — GitHub Action Template Guide

---

## Table of Contents

- [Overview](#overview)
- [What It Does](#what-it-does)
- [Advantages](#advantages)
- [Execution Modes](#execution-modes)
- [Supported Project Types](#supported-project-types)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [What You Must Change After Copying](#what-you-must-change-after-copying)
- [Configuring Repository Secrets](#configuring-repository-secrets)
- [Enabling Manual Controls](#enabling-manual-controls)
- [How the Workflow Works](#how-the-workflow-works)
- [Configuration Reference](#configuration-reference)
- [Best Practices](#best-practices)
- [Repository Onboarding Checklist](#repository-onboarding-checklist)
- [Sample Templates](#sample-templates)
- [Support](#support)

---

## Overview

This guide explains how to consume the Siemens Continuous Clearing GitHub Action templates to automate license compliance in your repository.

Two workflow templates are provided — choose the one that fits your environment:

| Template | File | Runner Requirement |
|---|---|---|
| **Binary (NuGet)** | `cctool-helper-binary.yml` | Any GitHub-hosted runner |
| **Docker** | `cctool-helper-docker.yml` | Runner with Docker daemon |

Copy your chosen template into your repository as:

```
.github/workflows/license-clearance.yml
```

---

## What It Does

Both templates automate the same three-stage license compliance pipeline:

| Stage | Purpose | Default |
|---|---|---|
| **Package Identifier** | Scans project dependencies and generates a CycloneDX SBOM | Always enabled |
| **SW360 Package Creator** | Creates or updates software components in SW360 | Disabled |
| **Artifactory Uploader** | Uploads cleared components to JFrog Artifactory | Disabled |

On every push or pull request the workflow automatically:

1. Restores project dependencies
2. Collects dependency metadata
3. Generates a standardized SBOM listing all direct and transitive dependencies
4. Uploads the SBOM as a downloadable workflow artifact

---

## Advantages

### No infrastructure setup

Both templates run entirely on GitHub-hosted runners. No self-hosted runners, no additional servers are needed.

### Safe defaults for CI

By default only the Package Identifier stage runs. Side-effect stages (SW360, Artifactory) remain disabled until explicitly enabled, making it safe to add to any repository immediately.

### Centralized maintenance

The actual clearing logic lives in a reusable workflow maintained in the [`siemens/continuous-clearing`](https://github.com/siemens/continuous-clearing) repository. Your repository only contains a thin caller workflow. Updates to the tool are consumed by changing a single version tag.

### Multi-language support

A single workflow template supports NuGet, NPM, Python (Poetry), Maven, and Conan projects. Only the restore command and artifact path need to be adjusted.

### Choose your execution model

Pick the Binary (NuGet) template for the simplest setup, or the Docker template when you need full container isolation. Both templates expose the same inputs, secrets, and stage toggles.

---

## Execution Modes

### Mode 1: Binary / NuGet

| Aspect | Detail |
|---|---|
| Template file | `cctool-helper-binary.yml` |
| Runner requirement | GitHub-hosted `ubuntu-latest` |
| Docker required | No |
| Installation | `dotnet tool install` (handled by the reusable workflow) |
| Startup time | Fast — no image pull |

**When to use:** Default choice for most repositories. Works on any GitHub-hosted runner without additional configuration.

### Mode 2: Docker

| Aspect | Detail |
|---|---|
| Template file | `cctool-helper-docker.yml` |
| Runner requirement | Runner with Docker daemon |
| Docker required | Yes |
| Installation | `docker pull` of the CC Tool image |
| Startup time | Slower — requires image download |

**When to use:** When your organization requires containerized tooling, when you need strict environment isolation, or when running on self-hosted runners with pre-cached images.

---

## Supported Project Types

| Project Type | Required Files | Restore Command |
|---|---|---|
| NuGet (.NET) | `*.csproj`, `project.assets.json` | `dotnet restore` |
| NPM (Node.js) | `package.json`, `package-lock.json` | `npm ci` |
| Poetry (Python) | `poetry.lock`, `pyproject.toml` | None (files must exist in repo) |
| Maven (Java) | `pom.xml` | `mvn install` |
| Conan (C++) | `conanfile.txt` or `conanfile.py` | `conan install` |

---

## Prerequisites

| Requirement | Purpose |
|---|---|
| GitHub repository with admin or maintainer access | Add workflow files and configure secrets |
| SW360 account with REST API token | Authenticate clearing operations |
| SW360 project created for your repository | Map components to a project |
| JFrog Artifactory token (optional) | Required only if Artifactory upload is enabled |
| Docker on the runner (Docker mode only) | Required to run the containerized CC Tool |

---

## Quick Start

### Step 1: Choose a template

Pick the template that matches your environment:

- **No Docker available or preferred:** use `cctool-helper-binary.yml`
- **Docker required or preferred:** use `cctool-helper-docker.yml`

### Step 2: Copy the template

Copy your chosen template into your repository as:

```
.github/workflows/license-clearance.yml
```

### Step 3: Update the restore command and artifact path

See [What You Must Change After Copying](#what-you-must-change-after-copying).

### Step 4: Add repository secrets

See [Configuring Repository Secrets](#configuring-repository-secrets).

### Step 5: Commit and push

```sh
git add .github/workflows/license-clearance.yml
git commit -m "Add Continuous Clearing workflow"
git push
```

The workflow runs automatically on push to `main` or `master` and on pull requests.

---

## What You Must Change After Copying

After copying either template, update two sections to match your repository.

### 1. Restore command

Replace the sample restore command with your project's actual path.

**Template default (both templates):**

```yaml
- name: Restore NuGet dependencies
  run: dotnet restore backend/backend.csproj
```

**Examples by project type:**

NuGet (single project):

```yaml
- name: Restore dependencies
  run: dotnet restore src/MyProject/MyProject.csproj
```

NuGet (solution):

```yaml
- name: Restore dependencies
  run: dotnet restore src/MySolution.sln
```

NPM:

```yaml
- name: Install dependencies
  run: npm ci
  working-directory: frontend
```

Poetry:

```yaml
- name: Verify Poetry files
  run: |
    test -f poetry.lock || (echo "poetry.lock not found" && exit 1)
    test -f pyproject.toml || (echo "pyproject.toml not found" && exit 1)
```

### 2. Artifact upload path

Replace the sample path so the uploaded artifact contains the dependency metadata.

**Template default (both templates):**

```yaml
- name: Upload CC input artifact
  uses: actions/upload-artifact@v4
  with:
    name: cc-input
    path: backend
    if-no-files-found: error
```

**Examples by project type:**

NuGet:

```yaml
path: src
```

NPM:

```yaml
path: frontend
```

Poetry:

```yaml
path: |
  poetry.lock
  pyproject.toml
```

> **Important:** The artifact must be named `cc-input`. Both reusable workflows expect this exact name.

---

## Configuring Repository Secrets

Navigate to: **Repository ? Settings ? Secrets and variables ? Actions ? New repository secret**

| Secret name | Purpose | How to obtain |
|---|---|---|
| `CC_SW360_TOKEN` | SW360 REST API authentication | SW360 ? Profile ? Preferences ? REST API Tokens ? Generate Token |
| `CC_JFROG_TOKEN` | JFrog Artifactory authentication | Artifactory ? Profile ? Generate API Key |

Using GitHub CLI:

```sh
gh secret set CC_SW360_TOKEN
gh secret set CC_JFROG_TOKEN
gh secret list
```

**Notes:**

- `CC_SW360_TOKEN` is required for Package Identifier enrichment and SW360 Package Creator
- `CC_JFROG_TOKEN` is required only when the Artifactory Uploader stage is enabled
- Both templates use `secrets: inherit` to forward all secrets to the reusable workflow

---

## Enabling Manual Controls

Both templates contain a commented `workflow_dispatch` section. Uncomment it to enable manual triggers with stage control from the GitHub Actions UI.

```yaml
workflow_dispatch:
  inputs:
    enable_sw360_package_creator:
      description: 'Run SW360 Package Creator'
      type: boolean
      default: false
    enable_artifactory_uploader:
      description: 'Run Artifactory Uploader (dry-run by default)'
      type: boolean
      default: false
    jfrog_dry_run:
      description: 'Artifactory dry-run mode (false = live copy/move for releases)'
      type: boolean
      default: true
```

To trigger manually:

1. Go to the **Actions** tab in GitHub
2. Select the **License Clearance** workflow
3. Click **Run workflow**
4. Set the desired input values
5. Click **Run workflow**

---

## How the Workflow Works

Both templates follow the same two-job structure.

### Job 1: `build`

Prepares the dependency metadata:

1. Checks out the repository
2. Sets up the required SDK (.NET 8, Node.js, etc.)
3. Restores dependencies
4. Verifies dependency metadata exists
5. Uploads the `cc-input` artifact

### Job 2: `clearing`

Delegates to the centralized reusable workflow:

1. Downloads the `cc-input` artifact
2. Installs the CC Tool (NuGet binary or Docker image, depending on template)
3. Runs Package Identifier (always enabled)
4. Runs SW360 Package Creator (if enabled)
5. Runs Artifactory Uploader (if enabled)
6. Uploads output artifacts (SBOM)

### Concurrency

Both templates cancel any in-progress run on the same branch or PR when a newer commit arrives:

**Binary template:**

```yaml
concurrency:
  group: license-clearance-binary-${{ github.ref }}
  cancel-in-progress: true
```

**Docker template:**

```yaml
concurrency:
  group: license-clearance-${{ github.ref }}
  cancel-in-progress: true
```

---

## Configuration Reference

### Workflow inputs (manual trigger only)

These inputs are identical for both templates:

| Input | Default | Description |
|---|---|---|
| `enable_sw360_package_creator` | `false` | Runs the SW360 Package Creator stage |
| `enable_artifactory_uploader` | `false` | Runs the Artifactory Uploader stage |
| `jfrog_dry_run` | `true` | Simulates Artifactory operations. Set to `false` only for approved releases |

### Trigger behavior by event

| Event | Package Identifier | SW360 Creator | Artifactory Uploader |
|---|---|---|---|
| Push to main/master | Always runs | Disabled | Disabled |
| Pull request | Always runs | Disabled | Disabled |
| Manual dispatch | Always runs | Controlled by input | Controlled by input |

---

## Best Practices

- **Choose the right template** — Use the Binary (NuGet) template unless your organization specifically requires Docker-based tooling.
- **Keep CI safe by default** — Only enable Package Identifier for everyday CI. Keep SW360 and Artifactory stages disabled unless explicitly needed.
- **Run on every pull request** — Catches license and compliance issues before merge.
- **Use manual triggers for side-effect stages** — SW360 creation and Artifactory upload should be triggered manually for controlled execution.
- **Pin reusable workflows to release tags** — Use `@v4.2.0` instead of `@main` or a feature branch for production repositories.
- **Validate restore locally** — Run the restore command locally before enabling the workflow to confirm dependency metadata is generated.
- **Keep artifact input focused** — Upload only the folders needed for dependency analysis to reduce artifact size and speed up runs.

---

## Repository Onboarding Checklist

- [ ] Choose a template: `cctool-helper-binary.yml` or `cctool-helper-docker.yml`
- [ ] Copy the chosen template to `.github/workflows/license-clearance.yml`
- [ ] Update the restore command to match your repository structure
- [ ] Update the artifact upload path to include dependency metadata folders
- [ ] Add `CC_SW360_TOKEN` to repository secrets
- [ ] Add `CC_JFROG_TOKEN` to repository secrets (if Artifactory stage is needed)
- [ ] Commit and push
- [ ] Verify the workflow runs on push and pull request
- [ ] Download and review the generated SBOM artifact
- [ ] Uncomment `workflow_dispatch` section if manual stage control is needed
- [ ] Pin the reusable workflow reference to a release tag before production rollout

---

## Sample Templates

### NuGet (.NET) — Binary Mode

```yaml
name: License Clearance (Binary)

on:
  push:
    branches: [main, master]
  pull_request:
  workflow_dispatch:
    inputs:
      enable_sw360_package_creator:
        description: 'Run SW360 Package Creator'
        type: boolean
        default: false
      enable_artifactory_uploader:
        description: 'Run Artifactory Uploader (dry-run by default)'
        type: boolean
        default: false
      jfrog_dry_run:
        description: 'Artifactory dry-run mode'
        type: boolean
        default: true

concurrency:
  group: license-clearance-binary-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build & Restore
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore src/MySolution.sln
      - run: find src -name project.assets.json
      - uses: actions/upload-artifact@v4
        with:
          name: cc-input
          path: src
          if-no-files-found: error

  clearing:
    name: Continuous Clearing
    needs: build
    uses: siemens/continuous-clearing/.github/workflows/continuous-clearing-binary.yml@v4.2.0
    secrets: inherit
    with:
      enable_sw360_package_creator: ${{ inputs.enable_sw360_package_creator || false }}
      enable_artifactory_uploader: ${{ inputs.enable_artifactory_uploader || false }}
      jfrog_dry_run: ${{ inputs.jfrog_dry_run || true }}
```

### NuGet (.NET) — Docker Mode

```yaml
name: License Clearance

on:
  push:
    branches: [main, master]
  pull_request:
  workflow_dispatch:
    inputs:
      enable_sw360_package_creator:
        description: 'Run SW360 Package Creator'
        type: boolean
        default: false
      enable_artifactory_uploader:
        description: 'Run Artifactory Uploader (dry-run by default)'
        type: boolean
        default: false
      jfrog_dry_run:
        description: 'Artifactory dry-run mode'
        type: boolean
        default: true

concurrency:
  group: license-clearance-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build & Restore
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore src/MySolution.sln
      - run: find src -name project.assets.json
      - uses: actions/upload-artifact@v4
        with:
          name: cc-input
          path: src
          if-no-files-found: error

  clearing:
    name: Continuous Clearing
    needs: build
    uses: siemens/continuous-clearing/.github/workflows/continuous-clearing.yml@v4.2.0
    secrets: inherit
    with:
      enable_sw360_package_creator: ${{ inputs.enable_sw360_package_creator || false }}
      enable_artifactory_uploader: ${{ inputs.enable_artifactory_uploader || false }}
      jfrog_dry_run: ${{ inputs.jfrog_dry_run || true }}
```

### NPM (Node.js) — Binary Mode

```yaml
name: License Clearance (Binary)

on:
  push:
    branches: [main, master]
  pull_request:
  workflow_dispatch:
    inputs:
      enable_sw360_package_creator:
        description: 'Run SW360 Package Creator'
        type: boolean
        default: false
      enable_artifactory_uploader:
        description: 'Run Artifactory Uploader (dry-run by default)'
        type: boolean
        default: false
      jfrog_dry_run:
        description: 'Artifactory dry-run mode'
        type: boolean
        default: true

concurrency:
  group: license-clearance-binary-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build & Restore
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: npm ci
        working-directory: frontend
      - uses: actions/upload-artifact@v4
        with:
          name: cc-input
          path: frontend
          if-no-files-found: error

  clearing:
    name: Continuous Clearing
    needs: build
    uses: siemens/continuous-clearing/.github/workflows/continuous-clearing-binary.yml@v4.2.0
    secrets: inherit
    with:
      enable_sw360_package_creator: ${{ inputs.enable_sw360_package_creator || false }}
      enable_artifactory_uploader: ${{ inputs.enable_artifactory_uploader || false }}
      jfrog_dry_run: ${{ inputs.jfrog_dry_run || true }}
```

### Python (Poetry) — Binary Mode

```yaml
name: License Clearance (Binary)

on:
  push:
    branches: [main, master]
  pull_request:
  workflow_dispatch:
    inputs:
      enable_sw360_package_creator:
        description: 'Run SW360 Package Creator'
        type: boolean
        default: false
      enable_artifactory_uploader:
        description: 'Run Artifactory Uploader (dry-run by default)'
        type: boolean
        default: false
      jfrog_dry_run:
        description: 'Artifactory dry-run mode'
        type: boolean
        default: true

concurrency:
  group: license-clearance-binary-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build & Verify
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Verify Poetry files
        run: |
          test -f poetry.lock || (echo "poetry.lock not found" && exit 1)
          test -f pyproject.toml || (echo "pyproject.toml not found" && exit 1)
      - uses: actions/upload-artifact@v4
        with:
          name: cc-input
          path: |
            poetry.lock
            pyproject.toml
          if-no-files-found: error

  clearing:
    name: Continuous Clearing
    needs: build
    uses: siemens/continuous-clearing/.github/workflows/continuous-clearing-binary.yml@v4.2.0
    secrets: inherit
    with:
      enable_sw360_package_creator: ${{ inputs.enable_sw360_package_creator || false }}
      enable_artifactory_uploader: ${{ inputs.enable_artifactory_uploader || false }}
      jfrog_dry_run: ${{ inputs.jfrog_dry_run || true }}
```

---

## Support

- GitHub Issues: [https://github.com/siemens/continuous-clearing/issues](https://github.com/siemens/continuous-clearing/issues)
- Documentation: [https://github.com/siemens/continuous-clearing/tree/main/doc](https://github.com/siemens/continuous-clearing/tree/main/doc)
