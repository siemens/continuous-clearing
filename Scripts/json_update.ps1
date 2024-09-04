# SPDX-FileCopyrightText: 2024 Siemens AG
# SPDX-License-Identifier: MIT

param(
	[Parameter(Mandatory=$true)]
	$JsonFile=""
	)
	

if (([string]::IsNullOrEmpty($env:SW360HOST)) -or ([string]::IsNullOrEmpty($env:SW360APPPORT)) -or ([string]::IsNullOrEmpty($env:FOSSYHOST)) -or ([string]::IsNullOrEmpty($env:FOSSYAPPPORT)))
{
	Write-Host "some of the parameters not set eg. sw360 host or port, fossology host or port"
	exit
}
else
{
	$sw360urlobj = -join ("http://", "${env:SW360HOST}", ":", "${env:SW360APPPORT}")
	write-host "The sw360 URL is $sw360urlobj"
	$fossyurlobj = -join ("http://", "${env:FOSSYHOST}", ":", "${env:FOSSYAPPPORT}")
	write-host "The fossology URL is $fossyurlobj"
}

if (Test-Path "$JsonFile")
{
	Write-Host "Modifying the settings file json: $JsonFile"
	$Data = Get-Content "$JsonFile" | ConvertFrom-Json
	$Data.FossologyURL = "$fossyurlobj"
	$Data.SW360URL = "$sw360urlobj"
	$Data.ArtifactoryUploadUser = "${env:USEREMAIL}"
	$Data.ArtifactoryUploadApiKey = "${env:ARTIFACTORYAPIKEY}"
	$Data.JfrogApi = "${env:JFROGURL}"
	$Data.SW360AuthTokenValue = "${env:SW360AUTHKEY}"
	$Data | ConvertTo-Json -Depth 2 | Out-File $JsonFile
}

else
{
	Write-Host "$JsonFile file does not exist"
}