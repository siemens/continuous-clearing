# SPDX-FileCopyrightText: 2023 Siemens AG
# SPDX-License-Identifier: MIT

docker exec -dt fossyngcodesiemenscntr1 service postgresql stop
docker stop fossyngcodesiemenscntr1
docker rm fossyngcodesiemenscntr1
Write-Host "List of containers BEFORE fossy start"
docker ps -a
docker run --name fossyngcodesiemenscntr1 --memory=7g -dt -p ${env:FOSSYAPPPORT}:80 ${env:DOCKERDEVARTIFACTORY}/energy-dev/software-clearing/fossy/ng390:latest
Start-Sleep -s 300
Write-Host "List of containers AFTER fossy start"
docker ps -a

