REM SPDX-FileCopyrightText: 2026 Siemens AG
REM SPDX-License-Identifier: MIT

@ECHO OFF
pushd %~dp0\ContinuousClearingTool.11.0.0
echo "Starting SIT.Scan"
cmd.exe /c SIT.Scan.exe --packageFilePath ..\NPMProject --bomFolderPath ..\BOM --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --artifactoryuploadApiKey <artifactoryuploadApiKey> --projectType <ProjectType> --JfrogApi <JfrogUrl> --sw360Url <sw360Url>

echo "Finishing SIT.Scan"

echo "Starting SIT.Create"
cmd.exe /c SIT.Create.exe --bomFilePath ..\BOM\NPMProject_Bom.cdx.json --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --sw360Url <sw360Url> --fossologyUrl <FossURL>

echo "Finishing SIT Create"

echo "Starting SIT.Upload"
cmd.exe /c SIT.Uplod.exe --bomfilepath "..\BOM\NPMProject_Bom.cdx.json" --artifactoryuploaduser <artifactoryuploaduser> --artifactoryuploadapikey <artifactoryuploadapikey> --jfrognpmdestreponame <DestRepoForPackageToBeCopied> --JfrogApi <JfrogUrl>

echo "Finishing SIT.Upload"
IF %ERRORLEVEL% NEQ 0 (
	echo Error while executing ContinuousClearingTool
    echo ERRORLEVEL: %ERRORLEVEL%
    echo Aborting
    EXIT /B %ERRORLEVEL%
)