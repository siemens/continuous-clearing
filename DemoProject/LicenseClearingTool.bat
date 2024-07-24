REM SPDX-FileCopyrightText: 2024 Siemens AG
REM SPDX-License-Identifier: MIT

@ECHO OFF
pushd %~dp0\LicenseClearingTool.7.0.0
echo "Starting Package Identifier"
cmd.exe /c Package Identifier.exe --packageFilePath ..\NPMProject --bomFolderPath ..\BOM --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --artifactoryuploadApiKey <artifactoryuploadApiKey> --projectType <ProjectType> --JfrogApi <JfrogUrl> --sw360Url <sw360Url>

echo "Finishing Package Identifier"

echo "Starting SW360Package Creator"
cmd.exe /c SW360PackageCreator.exe --bomFilePath ..\BOM\NPMProject_Bom.cdx.json --sw360Token <sw360token> --sW360ProjectID <ProjectId> --sW360AuthTokenType Bearer --sw360Url <sw360Url> --fossologyUrl <FossURL>

echo "Finishing SW360Package Creator"

echo "Starting Artifactory Uploader"
cmd.exe /c Artifactory Uploader.exe --bomfilepath "..\BOM\NPMProject_Bom.cdx.json" --artifactoryuploaduser <artifactoryuploaduser> --artifactoryuploadapikey <artifactoryuploadapikey> --jfrognpmdestreponame <DestRepoForPackageToBeCopied> --JfrogApi <JfrogUrl>

echo "Finishing Artifactory Uploader"
IF %ERRORLEVEL% NEQ 0 (
	echo Error while executing LicenseClearingTool
    echo ERRORLEVEL: %ERRORLEVEL%
    echo Aborting
    EXIT /B %ERRORLEVEL%
)