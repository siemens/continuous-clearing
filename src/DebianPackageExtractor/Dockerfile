#
# <copyright company="Siemens AG">
#   Copyright (C) Siemens AG 2023. All rights reserved. 
# </copyright>
# <license>MIT</license>
#

FROM debian:latest

WORKDIR /tmp/DebianImageClearing

RUN apt-get update
RUN apt-get -y install curl
RUN apt-get -y install dpkg-dev
RUN apt-get install zip unzip
RUN curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /tmp/DebianImageClearing 
RUN apt-get clean
