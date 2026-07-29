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
             /app/out/PatchedFiles

# Install runtime tooling, .NET 10 SDK, and syft in a single layer to keep the image small.
# - ca-certificates + curl are required for the .NET and syft installers
# - nodejs, npm, git, maven, dpkg-dev are runtime tooling used by CATool
# - openjdk-21-jre-headless is the JRE shipped in Debian 13 main (JDK 17 is not available)
# - syft v1.46.0 generates SBOMs for Debian image clearing
# - .NET 10 SDK is installed via Microsoft's dotnet-install.sh into /usr/share/dotnet
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
        openjdk-21-jre-headless && \
    curl -sSfL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    chmod +x /tmp/dotnet-install.sh && \
    /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    rm -f /tmp/dotnet-install.sh && \
    curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /opt/DebianImageClearing v1.46.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/*

# Copy the CATool build output (produced by `dotnet build -c Release`) into the image.
# Build output lands in `out/net10.0/` because csproj has <OutputPath>..\..\out</OutputPath>
# and AppendTargetFrameworkToOutputPath defaults to true.
COPY /out/net10.0 /app/out
