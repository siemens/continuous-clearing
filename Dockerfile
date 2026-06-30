# SPDX-FileCopyrightText: 2024 Siemens AG
# SPDX-License-Identifier: MIT

# .NET 10 SDK on Ubuntu 24.04 LTS 'noble'.
# .NET 10 dropped Debian 12 (bookworm); no GA bookworm-slim or trixie-slim tag is
# published (only :10.0-preview-trixie-slim exists). 'noble' is the APT-based LTS
# variant supported through 2029 and ships openjdk-17-jre-headless in its main repo.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble

WORKDIR /app/out

# Creating required directories
RUN mkdir -p /opt/DebianImageClearing \
             /mnt/Input \
             /mnt/Output \
             /etc/CATool \
             /app/out/PatchedFiles

# Install required packages and OpenJDK in a single RUN, then purge Python LAST.
# - nodejs, npm, git, maven, curl, dpkg-dev, openjdk-17-jre-headless are runtime tooling
# - syft v1.46.0 generates SBOMs for Debian image clearing
#   (Note: v1.x changed the syft-json schema vs. v0.x. CycloneDX/SPDX outputs are stable.)
# - Python 3.12 is removed AFTER all apt-get installs to avoid leaving apt in a
#   half-broken state that would fail any subsequent `apt-get install`.
RUN apt-get update && \
    apt-get -y install --no-install-recommends \
        nodejs \
        npm \
        git \
        maven \
        curl \
        dpkg-dev \
        openjdk-17-jre-headless && \
    curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /opt/DebianImageClearing v1.46.0 && \
    dpkg -r --force-depends python3-minimal             || true && \
    dpkg -r --force-depends libpython3.12-minimal:amd64 || true && \
    dpkg -r --force-depends libpython3.12-stdlib:amd64  || true && \
    dpkg -r --force-depends python3.12                  || true && \
    dpkg -r --force-depends python3.12-minimal          || true && \
    dpkg --purge libpython3.12-minimal:amd64            || true && \
    dpkg --purge python3.12-minimal                     || true && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENV PATH="/root/.local/bin:/opt/DebianImageClearing:$PATH"

# Copy the CATool build output (produced by `dotnet build -c Release`) into the image.
# Build output lands in `out/net10.0/` because csproj has <OutputPath>..\..\out</OutputPath>
# and AppendTargetFrameworkToOutputPath defaults to true.
COPY /out/net10.0 /app/out
