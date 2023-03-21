# SPDX-FileCopyrightText: 2023 Siemens AG
# SPDX-License-Identifier: MIT

# Get parent image as latest debian patch of bullseye
FROM mcr.microsoft.com/dotnet/runtime:6.0.7-bullseye-slim-amd64
WORKDIR /app/out

# Creating required directories
RUN mkdir /opt/DebianImageClearing && \
    mkdir /mnt/Input && \
    mkdir /mnt/Output && \
    mkdir /etc/CATool && \
    mkdir /app/out/PatchedFiles

# Installing required packages
RUN apt-get update && \
    apt-get -y install --no-install-recommends nodejs npm && \
    apt-get -y install --no-install-recommends git && \
    apt-get -y install --no-install-recommends maven && \
    apt-get -y install --no-install-recommends curl && \
    apt-get -y install --no-install-recommends dpkg-dev && \
    curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /opt/DebianImageClearing && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf archive.tar.gz

# Copying files from host to current working directory
COPY /out/net6.0 /app/out

# Displaying Usage Info
CMD cat CLIUsage.txt
