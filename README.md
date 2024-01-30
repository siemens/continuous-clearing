![Build & Test](https://github.com/siemens/continuous-clearing/workflows/Build%20&%20Test/badge.svg?branch=main)
![Docker-publish](https://github.com/siemens/continuous-clearing/workflows/Docker-publish/badge.svg?branch=main)
![Publish NuGet Packages](https://github.com/siemens/continuous-clearing/workflows/Publish%20NuGet%20Packages/badge.svg)



# Introduction 

The Continuous Clearing Tool scans and collects the 3rd party OSS components used in a NPM/NuGet/Maven/Python/Debian and uploads it to SW360 and Fossology by accepting respective project ID for license clearing. 

The tool helps the developer/project manager to enable the clearing process faster by reducing the 
manual effort of creating SW360 and FOSSology workflows.

### Continuous Clearing Tool for SBOM :


To secure overall DevOps supply chain, we need to ensure that the coding is secure and other mandatory security aspects is integrated in Software development lifecycle from beginning to end. 
To ensure such practises are in place, we need to provide software bill of material ( SBOM ) for every automated build in DevOps chain. This SBOM will contain all the first and 3rd party components details including dependencies such as development,transitive and internal.



This tool has been  logically split into 3 different executables that enable it to be used as separate modules as per the user's requirement.

**_Note: Continuous Clearing Tool internally uses [Syft](https://github.com/anchore/syft) for component detection for debian type projects._**

# SEPP Integration with Continuous Clearing Tool

The Continuous Clearing Tool incorporates SEPP tool functionalities, seamlessly integrated into the Artifactory uploader.
This integration ensures
- Software License Clearing is done.
- No pre-release versions of re-use components are used.
- Trace-ability is guaranteed

### What is SEPP tool performing currently ?

* Check for third-party packages in artifactory
* Move internal packages from energy-dev- to energy-release- repos/
* Clone Git repositories.
* Export JSON file for Long term Archiving (LTA-Export)
 
### What are the existing functionalities of Continuous Clearing Tool ?
 
* Check for third party packages
* Identification of correct source code from github
* Creating third party components in SW360
* Triggering source code scan in FOSSology
* Copy cleared third party components from remote repo to SIPARTY release repo.

### Which functionality of SEPP did Continuous Clearing adapt newly ?
 
* Move internal packages from energy-dev-* to energy-release-* repos
* Copy development dependency packages to siparty-devdep-* repos
 
### What happens to SEPP now ?

Currently LTA support is not provided for SBOM, hence until that is implemented SEPP will coexist with continuous clearing tool .Once the implementation is done SEPP will eventually phase out.
# Package Installation 

 ### Install from GitHub Release (Official)
#### Use container image

```bash
docker pull ghcr.io/siemens/continuous-clearing
 ```

#### Use Binary

Download the .nupkg file from [GitHub Releases](https://github.com/siemens/continuous-clearing/releases)
	

 # Execution via terminal
 
 The Continuous Clearing Tool has 3 executables.
 
you can run Continuous Clearing Tool as container or as a dotnet package,
 
<details>
<summary>Run as container</summary>
 
 Execute them in the following order to achieve the complete License clearing process.

1. **Package Identifier** - This executable takes Package file or a `cycloneDX BOM` as input and provides a SBOM file as output. For each of the component the dependency classification (development,internal) and the availability in jfrog artifactory is identified and added in the SBOM file.

 
```text
docker run --rm -it -v /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet PackageIdentifier.dll --settingsfilepath /etc/CATool/appSetting.json
 ```
 * Input (i.e., /path/to/InputDirectory -> place to keep input files)
 * Output (i.e.,/path/to/OutputDirectory -> resulted files will be stored here) 
 * Log (i.e., /path/to/logDirectory -> logs will be stored here) 
 * Configuration (i.e., /path/to/ConfigDirectory -> place to keep the Config files i.e [**appSetting.json**](/src/LCT.Common/appSettings.json)) 
 
 2. **SW360 Package Creator** - This executable expects the `CycloneDX BOM` as the input, creates the missing components/releases in SW360 and links all the components to the respective project in SW360 portal and triggers the fossology upload.

 `Note`: By default the SBOM contains both dev and non dev dependent components. Hence while creating the components in Sw360  make sure to set the *RemoveDevDependency* flag as `true` to skip creating the development dependent components.
 
 ```text
 docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet SW360PackageCreator.dll --settingsfilepath /etc/CATool/appSetting.json
```
 3. **Artifactory Uploader** - This executable takes `CycloneDX BOM` which is updated by the ` SW360PackageCreator.dll` as input and uploads the components that are already cleared (clearing state - "Report approved") to the SIPARTY release repo in Jfrog Artifactory.
 ```text
  docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSetting.json
  ```
</details>

<details>
<summary>Run as dotnet package</summary>
 
 Extract the downloaded .nupkg package , execute the following commands inside the tools folder.

 1. **Package Identifier** - This executable takes Package file as input and provides a CycloneDX BOM file as output. For each of the component the  dependency classification (development,internal) and the availability in jfrog artifactory is identified and added in the BOM file.
 
```text
  PackageIdentifier.exe --settingsfilepath /<Config_Path>/appSetting.json
 ```
 
 2. **SW360 Package Creator** - This executable expects the `CycloneDX BOM` as the input, creates the missing components/releases in SW360 and links all the components to the respective project in SW360 portal and triggers the fossology upload.

  `Note`: By default the SBOM contains both dev and non dev dependent components. Hence while creating the components in Sw360  make sure to set the *RemoveDevDependency* flag as `true` to skip creating the development dependent components.
 
 ```text
  SW360PackageCreator.exe --settingsfilepath /<Config_Path>/appSetting.json
```
 3. **Artifactory Uploader** - This executable takes `CycloneDX BOM` which is updated by the ` SW360PackageCreator.dll` as input and uploads the components that are already cleared (clearing state - "Report approved") to the SIPARTY release repo in Jfrog Artifactory.
 ```text
   ArtifactoryUploader.exe --settingsfilepath /<Config_Path>/appSetting.json
  ```

</details>


Detailed insight on configuration and execution is provided in [Usage Doc](doc/UsageDoc/CA_UsageDocument.md).
 
 **_Note: ArtifactoryUploader is not applicable for Debian clearing._**

# Development

These instructions will get the project up and running on your local machine for development and testing purposes.

#### Prerequisite

1. Download Visual Studio 2022.
2. Download Docker latest version.
3. Docker image of continuous Clearing tool to be loaded locally.



#### Building via .NET SDK

* Clone the repo in your local directory
* Inside the `src` folder, execute the following command to build the source code :

```bash
dotnet build --configuration Release
 ```
 
#### Creating Docker image

Execute the following command inside the project's root directory where the `Dockerfile` is present to create an image :

```bash
docker build -t <DockerImageName> -f Dockerfile .
 ```
 ![](doc/gifs/DockerBuild.gif)
 
 #### Creating Dotnet package

Execute the following command inside the project's root directory :

```bash
nuget pack CA.nuspec
 ```
 ![](doc/gifs/NugetBuild.gif)
 
# Contribute

Improvements are always welcome! Feel free to log a bug, write a suggestion or
contribute code via merge request. To build and test the solution locally you should have .NET Core 6 installed. All details are listed in our contribution guide.
See  [CONTRIBUTING.md](CONTRIBUTING.md).

# License

Code and documentation under [MIT License](LICENSE)

Third-party software components list: 
- [ReadmeOSS_continuous-clearing_nupkg](https://htmlpreview.github.io/?https://github.com/siemens/continuous-clearing/blob/main/ReadmeOSS_continuous-clearing_nupkg.html)
- [ReadmeOSS_continuous-clearing_DockerImage](https://htmlpreview.github.io/?https://github.com/siemens/continuous-clearing/blob/main/ReadmeOSS_continuous-clearing_DockerImage.html)
    
Copyright 2023 Siemens AG

