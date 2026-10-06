<!--
SPDX-FileCopyrightText: 2026 Siemens AG

SPDX-License-Identifier: MIT
-->

# Source-Code Download Process

This document describes how the Continuous Clearing Tool identifies the source-code URL for a
component and shows a full end-to-end example for every supported project type.

---

## Identify the Source Location from the Registry

For each component, based on its package identifier (PURL), the tool queries the official
registry or metadata source for that ecosystem.

| Project Type | PURL prefix | Where the source URL is looked up | Key URL(s) |
|---|---|---|---|
| **NPM** | `pkg:npm` | npm registry returns the repository URL, normalized to a GitHub URL | `https://registry.npmjs.org/` |
| **NuGet** | `pkg:nuget` | Download the `.nuspec` metadata, read repository URL + commit | `https://api.nuget.org/v3-flatcontainer/{name-lowercase}/{version}/{name-lowercase}.nuspec` |
| **Debian** | `pkg:deb` | Debian snapshot service; binary → source resolution (with retry), collects patches | `https://snapshot.debian.org/mr/` (metadata) + `https://snapshot.debian.org/archive/` (download) |
| **Alpine** | `pkg:apk` | Clone aports repo, read package `APKBUILD` recipe, extract source archive link | `https://github.com/alpinelinux/aports.git` |
| **Maven** | `pkg:maven` | Build the `-sources.jar` path on Maven Central | `https://repo.maven.apache.org/maven2/` |
| **Python/Poetry** | `pkg:pypi` | Query PyPI metadata, select source distribution (sdist) | `https://pypi.org/pypi/{pkg}/{version}/json` |
| **Conan** | `pkg:conan` | Fetch the recipe's `conandata.yml` from the Conan Center index (raw GitHub), look up the `sources:` entry by version | `https://raw.githubusercontent.com/conan-io/conan-center-index/master/recipes/{name}/all/conandata.yml` |
| **Cargo** | `pkg:cargo` | Query crates.io API by name+version; result is `repository;{dl_path}` | `https://crates.io/api/v1/crates/{name}/{version}` |

> Only archive extensions `.tar.gz`, `.tar.xz`, `.tgz`, and `.bz2` are accepted for Alpine.
>
> If no URL is found, the component is marked **"not found"**, skipped, and logged:
> *"Identification of SRC url failed... Exclude if it is an internal component or manually update the SRC url."*

> **Note (Debian only):** When the source code cannot be downloaded, the component/release is **not
> created in SW360**.

---

## Full Examples — All Project Types

### NuGet — `Newtonsoft.Json 12.0.3`
1. Read component → PURL `pkg:nuget/Newtonsoft.Json@12.0.3` → type **NuGet**.
2. Fetch metadata: `https://api.nuget.org/v3-flatcontainer/newtonsoft.json/12.0.3/newtonsoft.json.nuspec`
3. Extract repository URL + commit from the nuspec → `https://github.com/JamesNK/Newtonsoft.Json` + commit hash.
4. Save as source/download URL.
5. Download the source code (`git clone https://github.com/JamesNK/Newtonsoft.Json` and checkout the commit).
6. Upload source to the SW360 release.
7. `EnableTrigger = true` → send to Fossology for scanning.

### NPM — `rxjs 6.5.4`
1. Read component → PURL `pkg:npm/rxjs@6.5.4` → type **NPM**.
2. Query the npm registry: `https://registry.npmjs.org/` (asks for the repository URL).
3. Registry returns `git+https://github.com/ReactiveX/rxjs.git` → normalized to `https://github.com/ReactiveX/rxjs`.
4. Save as source/download URL.
5. Download the source code (`git clone https://github.com/ReactiveX/rxjs`).
6. Upload source to the SW360 release.
7. `EnableTrigger = true` → send to Fossology for scanning.

### Debian — `apt 3.0.3`
1. Read component → PURL `pkg:deb/debian/apt@3.0.3?arch=source` → type **Debian**.
2. Look up in Debian snapshot service: `https://snapshot.debian.org/mr/` (metadata) and `https://snapshot.debian.org/archive/` (download); binary → source resolution, with retry.
3. Resolves the `apt` source package; collects the source archive `.orig.tar.*` plus any patch files `.debian.tar.*`.
4. Save source URL + patch URLs.
5. Download the source code.
6. **Apply patches** (`.debian.tar.*`) to the source so it matches the released package.
7. Upload source to the SW360 release → Fossology scan if `EnableTrigger = true`.

### Alpine — `zlib 1.2.12-r3`
1. Read component → PURL `pkg:apk/alpine/zlib@1.2.12-r3?arch=source` → type **Alpine**.
2. Clone aports repo: `https://github.com/alpinelinux/aports.git` (checkout the matching `-stable` branch).
3. Read the build recipe `aports/main/zlib/APKBUILD` → extract the `source=` archive link (e.g. `https://zlib.net/zlib-1.2.12.tar.gz`).
4. Save as source URL.
5. Download the source code.
6. **Apply APKBUILD patch files** to the source.
7. Upload to the SW360 release → Fossology scan if `EnableTrigger = true`.

### Maven — `joda-time:joda-time:2.9.2`
1. Read component → PURL `pkg:maven/joda-time/joda-time@2.9.2` → type **Maven**.
2. Build the sources-jar path on Maven Central: base `https://repo.maven.apache.org/maven2/`.
3. Resolve to: `https://repo.maven.apache.org/maven2/joda-time/joda-time/2.9.2/joda-time-2.9.2-sources.jar`
4. Save as source/download URL.
5. Download the source code (the `-sources.jar`).
6. Upload source to the SW360 release.
7. `EnableTrigger = true` → send to Fossology for scanning.

### Python / Poetry — `html5lib 1.1`
1. Read component → PURL `pkg:pypi/html5lib@1.1` → type **Python/Poetry**.
2. Query PyPI metadata: `https://pypi.org/pypi/html5lib/1.1/json`.
3. Select the source distribution (sdist) → `https://files.pythonhosted.org/.../html5lib-1.1.tar.gz`.
4. Save as source/download URL.
5. Download the source code.
6. Normalize the archive format if needed (e.g., `.zip` → `.tar.gz`) for uniform upload.
7. Upload to the SW360 release → Fossology scan if `EnableTrigger = true`.

### Conan — `libcurl 8.18.0`
1. Read component → PURL `pkg:conan/libcurl@8.18.0` → type **Conan**.
2. Build the `conandata.yml` URL (base `SourceURLConan` + `{name}/all/conandata.yml`):
   `https://raw.githubusercontent.com/conan-io/conan-center-index/master/recipes/libcurl/all/conandata.yml`
3. Download and YAML-parse the file, then look up the `sources:` dictionary by the exact version key `8.18.0`.
   The entry's `url` may be a single string or a list — if it's a list, the first URL is taken (e.g. `https://curl.se/download/curl-8.18.0.tar.gz`). An accompanying `sha256` is also present.
4. Save as source/download URL.
5. Download the source code.
6. Upload source to the SW360 release.
7. `EnableTrigger = true` → send to Fossology for scanning.

### Cargo — `serde 1.0.226`
1. Read component → PURL `pkg:cargo/serde@1.0.226` → type **Cargo**.
2. Query the crates.io API by name **and** version:
   `https://crates.io/api/v1/crates/serde/1.0.226`
3. From the JSON `version` object, read `repository` and `dl_path`. The result is the two values joined with `;`:
   `https://github.com/serde-rs/serde;https://crates.io/api/v1/crates/serde/1.0.226/download`
   (`dl_path` is prefixed with the base `https://crates.io`). Either part may be empty.
4. Save as source/download URL.
5. Download the source code.
6. Upload source to the SW360 release.
7. `EnableTrigger = true` → send to Fossology for scanning.

---

## How Patches Are Applied

Only **Debian** and **Alpine** apply patch files after the source is downloaded. Each uses a
different mechanism.

### Alpine (uses `git apply`)
1. The downloaded source archive (`.gz` / `.bz2`) is un-gzipped into a working folder.
2. A git repository is initialized in that folder (`git init`) so patches can be applied cleanly.
3. The `APKBUILD` recipe lists the files that make up the package. Every entry ending in a patch
   extension (e.g., `.patch`) is located inside the cloned aports folder
   (`aports/main/{component}/{patchFile}`).
4. For each patch file, it is applied with `git apply {patchFile}` run from inside the source folder.
5. After all patches are applied, the temporary `.git` folder is deleted, leaving the patched source.

### Debian (uses `dpkg-source` inside Docker)
1. The Debian source is downloaded as a `.dsc` descriptor plus its `.orig.tar.gz` and
   `.debian.tar.xz` (the patches live inside the Debian tarball).
2. Patching runs inside a Docker container that has Debian tooling available.
3. The container runs `dpkg-source -x {dscFile} {componentName}`, which unpacks the original source
   **and automatically applies all Debian patches** in the correct order.
4. The resulting patched source folder is repackaged into a single combined archive
   (`tar -cjf {name}_{version}.combined... {componentName}/`) using deterministic tar options.
5. That combined, patched archive is what gets uploaded.

> In both cases, patching ensures the uploaded source matches the actually released package before
> it is sent to SW360/Fossology.

---

## Common Notes

- **Patches:** Only Debian and Alpine apply patch files after download (see above).
- **Fossology scan:** Triggered only when `SW360.Fossology.EnableTrigger = true` in `appSettings.json`
  (Fossology base URL comes from `SW360.Fossology.URL`).
