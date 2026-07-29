# SPDX-FileCopyrightText: 2024 Siemens AG
# SPDX-License-Identifier: MIT

# CATool runtime image on Debian 13 ('trixie') slim.
# Microsoft does not publish a GA Debian tag for .NET 10 SDK (only preview), so we
# start from the official debian:trixie-slim image and install the .NET 10 SDK
# using Microsoft's dotnet-install.sh script. This keeps the image on a pure
# Debian base with a smaller footprint than the Ubuntu 'noble' variant.
FROM debian:trixie-slim

# .NET install location and PATH updates
ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    PATH="/usr/share/dotnet:/root/.dotnet/tools:/root/.local/bin:/opt/DebianImageClearing:${PATH}"

WORKDIR /app/out

# Creating required directories
RUN mkdir -p /opt/DebianImageClearing \
             /mnt/Input \
             /mnt/Output \
             /etc/CATool \
             /var/log \
             /app/out/PatchedFiles && \
    chmod 1777 /var/log /mnt/Output

# Install runtime tooling, .NET 10 SDK, syft, and OpenJDK 17 in a single layer.
# - ca-certificates + curl are required for the .NET, syft, and openjdk downloads
# - nodejs, npm, git, maven, dpkg-dev are runtime tooling used by CATool
# - syft v1.46.0 generates SBOMs for Debian image clearing
# - .NET 10 SDK is installed via Microsoft's dotnet-install.sh into /usr/share/dotnet
# - OpenJDK 17 (17.0.9+9-1~deb12u1) is installed from the Debian snapshot archive
#   to preserve exact JDK parity with the previous .NET 8 image (Debian 13 main
#   ships only openjdk-21). java-common provides the /usr/bin/java alternatives
#   scaffolding required by the openjdk-17 .deb.
RUN apt-get update && \
    apt-get -y install --no-install-recommends \
        ca-certificates \
        curl \
        nodejs \
        npm \
        git \
        maven \
        dpkg-dev \
        libicu-dev \
        libssl-dev \
        libgssapi-krb5-2 \
        libunwind8 \
        tzdata \
        java-common && \
    curl -sSfL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    chmod +x /tmp/dotnet-install.sh && \
    /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    rm -f /tmp/dotnet-install.sh && \
    curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /opt/DebianImageClearing v1.46.0 && \
    curl -L -o /tmp/openjdk-17.deb https://snapshot.debian.org/archive/debian-security/20231105T195436Z/pool/updates/main/o/openjdk-17/openjdk-17-jre-headless_17.0.9+9-1~deb12u1_amd64.deb && \
    dpkg -i /tmp/openjdk-17.deb || apt-get -y install -f --no-install-recommends && \
    rm -f /tmp/openjdk-17.deb && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/*

# Copy the CATool build output (produced by `dotnet build -c Release`) into the image.
# Build output lands in `out/net10.0/` because csproj has <OutputPath>..\..\out</OutputPath>
# and AppendTargetFrameworkToOutputPath defaults to true.
COPY /out/net10.0 /app/out
