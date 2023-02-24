# SPDX-FileCopyrightText: 2023 Siemens AG
# SPDX-License-Identifier: MIT

docker exec -dt sw360confctnr1 service postgresql stop
docker stop sw360confctnr1
docker rm sw360confctnr1
Write-Host "List of containers BEFORE sw360 start"
docker ps -a
docker run --name sw360confctnr1 --memory=5g -t -d -p ${env:SW360APPPORT}:8080 -p 5985:5984 -p 5435:5432 ${env:DOCKERDEVARTIFACTORY}/energy-dev/software-clearing/sw360/sw360conf:latest
Start-Sleep -s 540
Write-Host "List of containers AFTER sw360 start"
docker ps -a
