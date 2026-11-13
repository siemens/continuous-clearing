# SPDX-FileCopyrightText: 2024 Siemens AG
# SPDX-License-Identifier: MIT

FROM debian:13-slim
ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:${DOTNET_ROOT}"
# Signal to the app (and match the official .NET images) that we're running inside a container.
# PipelineArtifactUploader uses this to skip ##vso[artifact.upload ...] commands, whose paths
# would otherwise reference files that only exist inside the container and not on the pipeline agent.
ENV DOTNET_RUNNING_IN_CONTAINER=true
WORKDIR /app/out
# Install the .NET 10 SDK via the official dotnet-install script.
# The SDK is required (not just runtime) because NuGet scanning invokes MSBuildLocator/MSBuild.
# The script is used instead of the packages.microsoft.com apt package because the apt repo lags
# behind the serviced SDK feature band (it still shipped 10.0.30x after that band left support).
# --channel 10.0 always resolves to the latest serviced 10.0.x SDK (same as UseDotNet@2 '10.0.x').
ARG DOTNET_CHANNEL=10.0

RUN apt-get update && \
   apt-get upgrade -y && \
   apt-get install -y --no-install-recommends \
       ca-certificates \
       curl \
       libicu76 \
       libssl3t64 \
       libgcc-s1 \
       libstdc++6 \
       zlib1g && \
   curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
   bash /tmp/dotnet-install.sh --channel "${DOTNET_CHANNEL}" --install-dir "${DOTNET_ROOT}" --no-path && \
   rm -f /tmp/dotnet-install.sh && \
   ln -sf "${DOTNET_ROOT}/dotnet" /usr/bin/dotnet && \
   dotnet --list-sdks && dotnet --list-runtimes && \
   apt-get purge -y --auto-remove curl && \
   rm -rf /var/lib/apt/lists/*
# Creating required directories
RUN mkdir -p \
   /opt/DebianImageClearing \
   /mnt/Input \
   /mnt/Output \
   /etc/CATool \
   /app/out/PatchedFiles

# Install required packages for CATool clearing flows.
# Node.js comes from the NodeSource 22.x LTS channel (Debian-packaged 20.x is EOL).
# Maven comes from the official Apache binary distribution instead of the Debian package, because the
# Debian package transitively bundles a vulnerable jsoup (< 1.23.2) via libjsoup-java.
# curl and gnupg are only needed at build time and are purged at the end.
ARG MAVEN_VERSION=3.9.11
ENV MAVEN_HOME=/opt/maven
ENV PATH="${MAVEN_HOME}/bin:${PATH}"
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
       gnupg && \
   mkdir -p /etc/apt/keyrings && \
   curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | \
       gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg && \
   echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" \
       > /etc/apt/sources.list.d/nodesource.list && \
   apt-get update && \
   apt-get install -y --no-install-recommends \
       nodejs \
       git \
       dpkg-dev \
       openjdk-21-jre-headless && \
   curl -fsSL -o /tmp/maven.tar.gz \
       "https://archive.apache.org/dist/maven/maven-3/${MAVEN_VERSION}/binaries/apache-maven-${MAVEN_VERSION}-bin.tar.gz" && \
   curl -fsSL -o /tmp/maven.tar.gz.sha512 \
       "https://archive.apache.org/dist/maven/maven-3/${MAVEN_VERSION}/binaries/apache-maven-${MAVEN_VERSION}-bin.tar.gz.sha512" && \
   echo "$(cat /tmp/maven.tar.gz.sha512)  /tmp/maven.tar.gz" | sha512sum -c - && \
   mkdir -p "${MAVEN_HOME}" && \
   tar -xzf /tmp/maven.tar.gz -C "${MAVEN_HOME}" --strip-components=1 && \
   rm -f /tmp/maven.tar.gz /tmp/maven.tar.gz.sha512 && \
   curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | \
       sh -s -- -b /opt/DebianImageClearing v1.46.0 && \
   apt-get purge -y --auto-remove curl gnupg && \
   rm -rf /var/lib/apt/lists/*
ENV PATH="/root/.local/bin:${PATH}"

# Copy the CATool build output (produced by `dotnet build -c Release`) into the image.
# Build output lands in `out/net10.0/` because csproj has <OutputPath>..\..\out</OutputPath>
# and AppendTargetFrameworkToOutputPath defaults to true.
COPY /out/net10.0 /app/out
