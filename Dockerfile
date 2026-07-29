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
# Install Microsoft package repository and the .NET 10 SDK.
# The SDK is required (not just runtime) because NuGet scanning invokes MSBuildLocator/MSBuild

RUN apt-get update && \
   apt-get install -y --no-install-recommends \
       wget \
       ca-certificates \
       gnupg \
       apt-transport-https && \
   wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb && \
   dpkg -i packages-microsoft-prod.deb && \
   rm packages-microsoft-prod.deb && \
   apt-get update && \
   apt-get install -y --no-install-recommends \
       dotnet-sdk-10.0 && \
   apt-get purge -y --auto-remove wget gnupg apt-transport-https && \
   rm -rf /var/lib/apt/lists/*
# Creating required directories
RUN mkdir -p \
   /opt/DebianImageClearing \
   /mnt/Input \
   /mnt/Output \
   /etc/CATool \
   /app/out/PatchedFiles

# `curl` is only needed to install Syft below, so it is purged in the same layer.
RUN apt-get update && \
   apt-get install -y --no-install-recommends \
       git \
       maven \
       curl \
       dpkg-dev \
       openjdk-21-jre-headless && \
   curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | \
       sh -s -- -b /opt/DebianImageClearing v1.46.0 && \
   apt-get purge -y --auto-remove curl && \
   rm -rf /var/lib/apt/lists/*
ENV PATH="/root/.local/bin:${PATH}"

# Copy the CATool build output (produced by `dotnet build -c Release`) into the image.
# Build output lands in `out/net10.0/` because csproj has <OutputPath>..\..\out</OutputPath>
# and AppendTargetFrameworkToOutputPath defaults to true.
COPY /out/net10.0 /app/out
