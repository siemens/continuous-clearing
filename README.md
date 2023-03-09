![Build & Test](https://github.com/siemens/continuous-clearing/workflows/Build%20&%20Test/badge.svg?branch=Pipeline_creation)






# Introduction 

The Clearing Automation Tool scans and collects the 3rd party OSS components used in a NPM/NuGet/Debian project and uploads it to SW360 and Fossology by accepting respective project ID for license clearing. 
The tool helps the developer/project manager to enable the clearing process faster by reducing the 
manual effort of creating SW360 and FOSSology workflows.

This tool has been  logically split into 3 different executables that enable it to be used as separate modules as per the user's requirement.

**_Note: CA Tool internally uses [Syft](https://github.com/anchore/syft) for component detection for debian type projects._**

# Prerequisite
 
 -  Docker latest version
  

 # Usage
 
### Consuming the Released Packages

Docker image or the CA Nuget package can be directly downloaded and used in your pipelines for faster clearing process.

### Building the Source and generating CA Docker Image

 1. Clone the repo to your local machine 
 2. Build the source code
 3. Create an image using the command below

    ` docker build -t sw30clearingautomationtool -f Dockerfile .` 
    
### Building the Source and generating CA Nuget Package
 
 1. Clone the repo to your local machine 
 2. Build the source code
 3. Create a nuget package by running the command given below in the repo where CA.nuspec is present.
    
       `nuget pack CA.nuspec`

 ### Execution via terminal

The Clearing Automation Tool has 3 dll 's.

Execute them in the following order to achieve the complete License clearing process.

 
 1. **Package Identifier** - This executable takes `package-lock.json` or a `cycloneDX BOM` as input and provides a CycloneDX BOM file as output. For each of the component the availability in jfrog artifactory is identified and added in the BOM file.
 
 ##### For Docker Image 
```text
docker run --rm -it /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool sw30clearingautomationtool dotnet PackageIdentifier.dll --settingsfilepath /etc/CATool/appSetting.json
 ```
 * Input (i.e., /path/to/InputDirectory -> place to keep input files)
 * Output (i.e.,/path/to/OutputDirectory -> resulted files will be stored here) 
 * Log (i.e., /path/to/logDirectory -> logs will be stored here) 
 * Configuration (i.e., /path/to/ConfigDirectory -> place to keep the Config files i.e **appSetting.json**) 

##### For Nuget Package

```text
PackageIdentifier.exe --packagefilepath <project directory path> --bomfolderpath <folder path to create BOM file> --sw360token <SW360 Auth token> --sw360projectid <projectfromsw360> --sW360authtokentype Bearer --artifactoryapikey <Jfrog Auth Token> --sw360url <sw360 Url>
```

**Argument List** : Below is the list of settings can be made in **appSetting.json** file.
 ```bash
 --packagefilepath           Path to the package-lock.json file or to the directory where the project is present in case we have multiple package-lock.json files.
 --cycloneDxbomfilePath      Path to the cycloneDx BOM file. This should not be used along with the package file path.Please note to give only one type of input at a time.
 --bomfolderpath             Path to keep the generated boms
 --sw360token                SW360 Auth Token. Make sure to pass this in a secure way so that critical credentials are not exposed.
 --sw360projectname          Name of the project created in SW360. _Note: Project name is case sensitive and should be same as it is in SW360, else execution will be aborted
 --projecttype               Type of the package
 --removedevdependency       Make this field to "true" , if Dev dependencies needs to be excluded from clearing
 --sw360url                  SW360 URL
 --sw360authtokentype        SW360 Auth Token Type.
 --bomfilepath               The file path of the *_comparisonBom.json file   
 --fossologyUrl 	            Fossology URL
 --EnableFossTrigger	    True (Default)      
 --artifactoryuploaduser     Jfrog User Email
 --jfrognpmdestreponame      The destination folder name for the NPM package to be copied to         
 --jfrognugetdestreponame    The destination folder name for the NUGET package to be copied to
 --artifactoryuploadapikey   Jfrog User Auth Token.  Make sure to pass this in a secure way so that critical credentials are not exposed.
 --timeout                   SW360 response timeout value 
 ```
 
 2. **SW360 Package Creator** - This executable expects the `CycloneDX BOM` as the input, creates the missing components/releases in SW360 and links all the components to the respective project in SW360 portal and triggers the fossology upload.
 
 ##### For Docker Image
 
 ```text
 docker run --rm -it /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool sw30clearingautomationtool dotnet SW360PackageCreator.dll --settingsfilepath /etc/CATool/appSetting.json
```

##### For Nuget Package

```text
SW360PackageCreator.exe --bomfilepath <CycloneDXBom file path> --sw360token <SW360 Auth token> --sw360projectid <sw360Project id> --sw360authtokentype Bearer --sw360url <sw360 url> --fossologyUrl <fossology Url >
```

 3. **Artifactory Uploader** - This executable takes `CycloneDX BOM` which is updated by the ` SW360PackageCreator.dll` as input and uploads the components that are already cleared (clearing state - "Report approved") to the SIPARTY release repo in Jfrog Artifactory.
 
  ##### For Docker Image
 ```text
  docker run --rm -it /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool sw30clearingautomationtool dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSetting.json
  ```
##### For Nuget Package

```text
ArtifactoryUploader.exe --bomfilepath <cyclonedx bom file path which is the result of Package Creator> --artifactoryuploaduser <user email> --artifactoryuploadapikey <Jfrog token> --jfrognpmdestreponame <npm destination repo name> --Jfrogapi <Siemens Jfrog artifactory url>
```

Detailed insight on configuration and execution is provided in [Usage Doc](UsageDoc/CA_UsageDocument.md).

# Development

These instructions will get the project up and running on your local machine for development and testing purposes.

#### Prerequisite

1. Download Visual Studio 2022.
2. Download Docker latest version.
3. Docker image of Clearing Automation tool to be loaded locally.



#### Building via .NET SDK

* Clone the repo in your local directory
* Inside the `src` folder, execute the following command to build the source code :

```bash
dotnet build --configuration Release
 ```
 
#### Deployment

Execute the following command inside the project's root directory where the `Dockerfile` is present to make an image :

```bash
docker build -t <DockerImageName> -f Dockerfile .
 ```
# Contribute

Improvements are always welcome! Feel free to log a bug, write a suggestion or
contribute code via merge request. To build and test the solution locally you should have .NET Core 6 installed. All details are listed in our contribution guide.
See  [CONTRIBUTING.md](CONTRIBUTING.md).

# License

Code and documentation Copyright 2023 Siemens AG
