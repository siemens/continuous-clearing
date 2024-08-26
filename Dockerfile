# SPDX-FileCopyrightText: 2024 Siemens AG
# SPDX-License-Identifier: MIT

# Get parent image as latest debian patch of bullseye
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim
WORKDIR /app/out

# Creating required directories
RUN mkdir /opt/DebianImageClearing && \
    mkdir /mnt/Input && \
    mkdir /mnt/Output && \
    mkdir /etc/CATool && \
    mkdir /app/out/PatchedFiles

# Installing required packages
# Installing syft:v0.90.0
# Installing specific version of openjdk
RUN apt-get update && \
    apt-get -y install --no-install-recommends nodejs npm && \
    apt-get -y install --no-install-recommends git && \
    apt-get -y install --no-install-recommends maven && \
    apt-get -y install --no-install-recommends curl && \
    apt-get -y install --no-install-recommends dpkg-dev && \   
    curl -o openjdk-17-jre-headless_17.0.9+9-1~deb12u1_amd64.deb https://snapshot.debian.org/archive/debian-security/20231105T195436Z/pool/updates/main/o/openjdk-17/openjdk-17-jre-headless_17.0.9+9-1~deb12u1_amd64.deb && \
    dpkg -i openjdk-17-jre-headless_17.0.9+9-1~deb12u1_amd64.deb && \
    curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /opt/DebianImageClearing v0.90.0 && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf archive.tar.gz

ENV PATH="/root/.local/bin:$PATH"

# Copying files from host to current working directory
#COPY /out/net8.0 /app/out
COPY /buildoutput/ /app/out
