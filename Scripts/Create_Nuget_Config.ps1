# SPDX-FileCopyrightText: 2023 Siemens AG
# SPDX-License-Identifier: MIT

param(
	$energydevnuget="xyz",
	$energydevnugetegll="xyz")

$nugetconfig = "$PSScriptRoot\..\nuget.config"

Write-Host "Remove existing Nuget Config if any"
if (Test-Path "$nugetconfig") 
{
	Remove-Item "$nugetconfig" -Force
}

New-Item -ItemType File -Force -Path "$nugetconfig"

Write-Host "The energy dev nuget URL is       : $energydevnuget"
Write-Host "The energy dev nuget egll  URL is : $energydevnugetegll"

Write-Host "Write base config"

$xmlBase = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <add key="energy-dev-nuget" value="$energydevnuget" />
    <add key="energy-dev-nuget-egll" value="$egllenergydevnuget" />
 </packageSources>
</configuration>
'@

$xmltemp = ($xmlBase.Replace('$energydevnuget', "$energydevnuget")).Replace('$egllenergydevnuget', "$energydevnugetegll")
#$xmlBase.Replace('$egllenergydevnuget', "$egllenergydevnuget")
Set-Content -Path "$nugetconfig" -Value $xmltemp

<# 
$tempXMLcont = Get-Content "$nugetconfig"
$tempXMLcont.replace('$energydevnuget',"$energydevnuget") | set-content "$nugetconfig" -force

$tempXMLcont = Get-Content "$nugetconfig"
$tempXMLcont.replace('$egllenergydevnuget',"$energydevnugetegll") | set-content "$nugetconfig" -force #>
