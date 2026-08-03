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
# Install the .NET 10 SDK from Microsoft's tarball (not via apt) so the SDK
# is not tracked by dpkg. This matches the behavior of the old
# mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim base image and keeps the
# generated SBOM free of dotnet-* .deb entries.
# The SDK is required (not just runtime) because NuGet scanning invokes MSBuildLocator/MSBuild.
ARG DOTNET_SDK_VERSION=10.0.100
# TODO: populate DOTNET_SDK_SHA512 from https://github.com/dotnet/core/tree/main/release-notes/10.0
# to enable checksum verification of the downloaded tarball.
ARG DOTNET_SDK_SHA512=

RUN apt-get update && \
   apt-get install -y --no-install-recommends \
       wget \
       ca-certificates \
       libicu76 \
       libssl3 \
       libstdc++6 \
       zlib1g && \
   wget -O dotnet.tar.gz \
       "https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_SDK_VERSION}/dotnet-sdk-${DOTNET_SDK_VERSION}-linux-x64.tar.gz" && \
   if [ -n "${DOTNET_SDK_SHA512}" ]; then \
       echo "${DOTNET_SDK_SHA512}  dotnet.tar.gz" | sha512sum -c -; \
   fi && \
   mkdir -p "${DOTNET_ROOT}" && \
   tar -oxzf dotnet.tar.gz -C "${DOTNET_ROOT}" && \
   ln -s "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet && \
   rm dotnet.tar.gz && \
   apt-get purge -y --auto-remove wget && \
   rm -rf /var/lib/apt/lists/*
# Creating required directories
RUN mkdir -p \
   /opt/DebianImageClearing \
   /mnt/Input \
   /mnt/Output \
   /etc/CATool \
   /app/out/PatchedFiles

# Install required packages for CATool clearing flows.# 
RUN apt-get update && \
   apt-get install -y --no-install-recommends \
       nodejs \
       npm \
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
