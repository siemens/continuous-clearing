REM SPDX-FileCopyrightText: 2024 Siemens AG
REM SPDX-License-Identifier: MIT

@ECHO OFF
pushd %~dp0\ContinuousClearingTool.7.0.0
echo "Starting SIT Scan"
cmd.exe /c SITScan.exe --packageFilePath ..\NPMProject --bomFolderPath ..\BOM --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --artifactoryuploadApiKey <artifactoryuploadApiKey> --projectType <ProjectType> --JfrogApi <JfrogUrl> --sw360Url <sw360Url>

echo "Finishing SIT Scan"

echo "Starting SIT Create"
cmd.exe /c SITCreate.exe --bomFilePath ..\BOM\NPMProject_Bom.cdx.json --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --sw360Url <sw360Url> --fossologyUrl <FossURL>

echo "Finishing SIT Create"

echo "Starting SIT Upload"
cmd.exe /c SITUplod.exe --bomfilepath "..\BOM\NPMProject_Bom.cdx.json" --artifactoryuploaduser <artifactoryuploaduser> --artifactoryuploadapikey <artifactoryuploadapikey> --jfrognpmdestreponame <DestRepoForPackageToBeCopied> --JfrogApi <JfrogUrl>

echo "Finishing SIT Upload"
IF %ERRORLEVEL% NEQ 0 (
	echo Error while executing ContinuousClearingTool
    echo ERRORLEVEL: %ERRORLEVEL%
    echo Aborting
    EXIT /B %ERRORLEVEL%
)