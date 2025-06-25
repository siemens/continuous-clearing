# Continuous Clearing Tool

## Contents
- [Continuous Clearing Tool](#continuous-clearing-tool)
  - [Contents](#contents)
- [Introduction](#introduction)
  - [Key Features](#key-features)
  - [Benefits](#benefits)
  - [Usage](#usage)
- [Continuous Clearing Tool workflow diagram](#continuous-clearing-tool-workflow-diagram)
- [Prerequisite](#prerequisite)
  - [Pipeline Configuration](#pipeline-configuration)
- [Installation Guide](#installation-guide)
    - [Option 1: Docker Container Image](#option-1-docker-container-image)
    - [Option 2: NuGet Package](#option-2-nuget-package)
- [Demo project after consuming the package](#demo-project-after-consuming-the-package)
- [Continuous Clearing Tool Execution](#continuous-clearing-tool-execution)
    - [Overview](#overview)
    - [**Prerequisite for Continuous Clearing Tool execution**](#prerequisite-for-continuous-clearing-tool-execution)
    - [**Configuring the Continuous Clearing Tool**](#configuring-the-continuous-clearing-tool)
      - [**Method 1 - Only AppSettings**](#method-1---only-appsettings)
    - [Below rows repeat for each supported package type.](#below-rows-repeat-for-each-supported-package-type)
      - [**Method 2 - Only Cmd paramaters**](#method-2---only-cmd-paramaters)
      - [**Method 3 (Recommended) - AppSettings + Cmd paramaters**](#method-3-recommended---appsettings--cmd-paramaters)
      - [Exclude  Component or Folders :](#exclude--component-or-folders-)
    - [**Continuous Clearing Tool Execution**](#continuous-clearing-tool-execution-1)
    - [Prerequisite](#prerequisite-1)
    - [Package Identifier](#package-identifier)
    - [SW360 Package Creator](#sw360-package-creator)
    - [Artifactory Uploader](#artifactory-uploader)
    - [Prerequisite](#prerequisite-2)
    - [Package Identifier](#package-identifier-1)
    - [SW360 Package Creator](#sw360-package-creator-1)
    - [Artifactory Uploader](#artifactory-uploader-1)
- [Artifactory Uploader Release Execution](#artifactory-uploader-release-execution)
- [How to handle multiple project types in same project](#how-to-handle-multiple-project-types-in-same-project)
    - [Example Steps for Execution:](#example-steps-for-execution)
- [Templates](#templates)
  - [Azure DevOps](#azure-devops)
    - [**Advantages of Running CA Tool via Templates**](#advantages-of-running-ca-tool-via-templates)
  - [Integration](#integration)
    - [Add a New Template Calling Step](#add-a-new-template-calling-step)
      - [NuGet Template Example](#nuget-template-example)
      - [Docker Template Example](#docker-template-example)
    - [Project Definitions Structure](#project-definitions-structure)
  - [Configuration Parameters](#configuration-parameters)
    - [Common Parameters](#common-parameters)
    - [Binary Template Specific Parameters](#binary-template-specific-parameters)
    - [Docker Template Specific Parameters](#docker-template-specific-parameters)
- [Troubleshoot](#troubleshoot)
- [Manual Update](#manual-update)
- [Bug or Enhancements](#bug-or-enhancements)
- [Glossary of Terms](#glossary-of-terms)
- [References](#references)
  - [Image References](#image-references)
  - [API References](#api-references)


# Introduction
Welcome to the Continuous Clearing Tool, your automated solution for streamlining the SW360 clearing process. Designed with Project Managers and Developers in mind, this tool efficiently manages third-party components across various platforms, including NPM, NuGet, Maven, Python, Conan, Alpine, and Debian.

## Key Features
- **Automated Scanning and Identification**: The tool automatically scans and identifies third-party components in your projects.
- **Integration with SW360**: It creates entries in SW360 for any components not already present, linking them to their respective projects.
- **FOSSology Code Scanning**: Initiates jobs for code scans in FOSSology, ensuring compliance and thorough analysis.
- **SBOM Generation**: Produces a Software Bill of Materials (SBOM) file detailing the nested descriptions of software artifact components and associated metadata.

## Benefits
- **Efficiency in Component Management**: Reduces the manual effort required to create and manage components in SW360.
- **Error Reduction**: Minimizes the risk of manual errors while creating components and identifying the correct version of source codes from public repositories.
- **Harmonized Component Creation**: Streamlines and harmonizes the creation of third-party components by automatically filling in necessary information in SW360.

## Usage
Simply integrate the Continuous Clearing Tool into your project workflow to experience seamless clearing processes and enhanced productivity.

# Continuous Clearing Tool workflow diagram
* Package Identifier
  * [NPM/NUGET/MAVEN/PYTHON/CONAN](../usagedocimg/packageIdentifiernpmnuget.PNG)
  * [Debian/Alpine](../usagedocimg/packageIdentifierdebianalpine.PNG)
  * [BasicSBOM](../usagedocimg/PackageidentifierBasicSBOMflowdiagram.png)
 
* SW360 Package Creator
  * [NPM/NUGET/MAVEN/PYTHON/CONAN](../usagedocimg/packageCreatirnpmnuget.PNG)
  * [Debian](../usagedocimg/packagecreatordebian.PNG)
  * [Alpine](../usagedocimg/ComponentcreaterforAlpine.PNG)

* Artifactory Uploader
  * [NPM/NUGET/MAVEN/PYTHON/CONAN](../usagedocimg/artifactoryuploader.PNG)

# Prerequisite
To ensure a smooth operation of the Continuous Clearing Tool, please follow these prerequisites:

1. **Project Entry in SW360**: 
   - Make sure your project is registered in SW360 for license clearance and is set to an **Active** state when running the Continuous Clearing Tool.
2. **Access Requirements**:
   - **SW360 REST API Authentication Token**:
     - **SW360 Token**:
       1. Users can generate a token from their functional account.
       2. Required credentials include the client ID and client secret.
   - **Artifactory Token**:
     - Necessary for uploading cleared, internal, and development packages into JFrog Artifactory. Users must obtain their own JFrog Artifactory token.

## Pipeline Configuration
For certain scenarios, the tool uses predefined exit codes, which are described below:

| Exit Code | Scenario                              |
| --------- | ------------------------------------- |
| 0         | Success                               |
| 1         | Critical failure/error in the run     |
| 2         | Action item required from user's side |

When configuring the Continuous Clearing Tool in the pipeline, users can set up each stage to reflect results based on these exit codes. This configuration can be implemented by the configuration management team during pipeline modification to support the Continuous Clearing Tool.

Once configured, your pipeline will look something like this:
![Pipeline Configuration Example](../usagedocimg/piplinepic.PNG)

# Installation Guide
Setting up the Continuous Clearing Tool is straightforward with these installation methods:

### Option 1: Docker Container Image
Deploy the Continuous Clearing Tool instantly using Docker by pulling the latest container image with the following command:

```bash
docker pull ghcr.io/siemens/continuous-clearing:latest
```

### Option 2: NuGet Package
Integrate the tool into your .NET projects by downloading the NuGet package:
- Visit the .nupkg file from the GitHub releases page to obtain the latest version of the package.


# Demo project after consuming the package
Dive into practical examples of how the Continuous Clearing Tool can be integrated and utilized within your projects. We provide sample YAML files that demonstrate the tool's setup and functionality.

Explore these configurations in the [DemoProject](../../DemoProject). These samples are crafted to guide and inspire your pipeline configurations, ensuring a smooth and effective integration process after consuming the package.

# Continuous Clearing Tool Execution
### Overview
The Continuous Clearing Tool comprises three executable DLLs, each playing a crucial role in achieving a comprehensive license clearing process. Execute them sequentially as listed below:

**Note** :The SBOM created by this tool follows the CycloneDX version [v1.6](https://cyclonedx.org/docs/1.6/json/) and Siemens SBOM standard [v3](https://sbom.siemens.io/v3/format.html). These formats ensure the SBOM is detailed, secure, and meets industry and Siemens-specific requirements.

> **1. Package Identifier**
> - This DLL processes the input file and generates a CycloneDX BOM file. The input can be a package file or a CycloneDX BOM file created using a standard tool. If multiple input files are present, simply provide the path to the directory as an argument.

**Functionality Without Connections:**
Users have the flexibility to generate a basic SBOM even if connections to SW360, JFrog, or both are unavailable. The tool maintains essential SBOM generation functionality with limited capabilities in such scenarios.

> **2. SW360 Package Creator**
> - Processes the SBOM file (output from the Package Identifier) to create missing components/releases in SW360 and link all components to the project within the SW360 portal. This executable also triggers the upload of components to Fossology and automatically updates the clearing state in SW360.
> 
`Note: Since the Package Identifier generates an SBOM file with Dev dependencies and internal components, ensure the RemoveDevDependency flag is set to true when executing this DLL.`

> **3. Artifactory Uploader**
> - This DLL processes the CycloneDX BOM file generated by the SW360 Package Creator. It targets components with a cleared status ("Report approved") and facilitates their transfer from a remote repository to the configured third-party repository in JFrog Artifactory. Additionally, it manages the transfer of development components from the remote repository to the designated development repository. Internal packages are relocated to the configured release repository.

`Note: The default setting for the JFrog dry run is true. This flag is intended to perform a dry run of the component copy/move operation, verifying the components' paths and permissions before executing the actual operation.`

### **Prerequisite for Continuous Clearing Tool execution**
* Input files according to project type

  * **Project Type :** **NPM**

    * Input file repository should contain **package-lock.json** file. If not present do an npm install.
      ![folderpic](../usagedocimg/npminstall.PNG)

  * **Project Type :** **Nuget**

    * .Net core/.Net standard type project's input file repository should contain **project.assets.json** file. If not present do a dotnet restore.

    * .Net Framework projects, input file repository should contain a **packages.config** file.

  * **Project Type :** **Maven**

    * [Apache Maven](https://dlcdn.apache.org/maven/maven-3/3.9.0/binaries/apache-maven-3.9.0-bin.zip) has to be installed in the build machine and added in the PATH variable.
      *Add the cycloneDX Maven Plugin to the main pom.xml* and run the command to generate the input bom file.

      ```
       mvn install cyclonedx:makeAggregateBom
      ```

    * Input file repository should contain **bom.cdx.json** file,Which will be the output of CycloneDx-Maven-Plugin tool

    * **Note** : Incase your project has internal dependencies, compile the project **prior to running the clearing tool**

      ```
        mvn clean install -DskipTests=true 
      ```

  * **Project Type :** **Python**

    * Input file repository should contain **poetry.lock** file.

  * **Project Type :** **Conan**

    * Input file repository should contain **conan.lock** file.

  * **Project Type :**  **Debian & Alpine**

    **Note** : below steps is required only if you have tar file to process , otherwise you can keep CycloneDx.json file in the Input Directory.

    * Create InputImage directory for keeping tar images and InputDirectory for resulted file storing .

    * Run the command given below by replacing the place holder values (i.e., path to input image directory, path to input directory and file name of the Debian image to be cleared) with actual values.

      **Example**:   `docker run --rm -v <path/to/InputImageDirectory>:/tmp/InputImages -v <path/to/InputDirectory>:/tmp/OutputFiles ghcr.io/siemens/continuous-clearing /opt/DebianImageClearing/./syft /tmp/InputImages/<fileNameoftheImageTobeCleared.tar> -o cyclonedx-json --file "/tmp/OutputFiles/output.sbom.cdx.json"`

      After successful execution, output.sbom.cdx.json (*CycloneDX.json*) file will be created in specified directory

      Resulted output.sbom.cdx.json file will be having the list of installed packages  and the same file will be used as  an input to Continuous clearing tool - Package identifier via the input directory parameter. The remaining process is same as other project types.


### **Configuring the Continuous Clearing Tool**

Arguments can be provided to the tool in two ways :

#### **Method 1 - Only AppSettings**

Copy content from the [sample app settings](https://github.com/siemens/continuous-clearing/blob/main/src/LCT.Common/appSettings.json) and create a new appSettings.json file in Continuous Clearing tool Config directory.

The appsettings can be passed to the tool via the command line paramater `--settingsfilepath`. The structure of the app settings can be fouond [here](https://github.com/siemens/continuous-clearing/blob/main/src/LCT.Common/appSettings.json).

Description for the settings in appSettings.json file

| S.No | Argument Name                             | Description                                                   | Mandatory | Example                                                                  |
| ---- | ----------------------------------------- | ------------------------------------------------------------- | --------------- | ------------------------------------------------------------------------ |
| 1    | TimeOut                                   | Timeout in seconds                                            | No              | 400                                                                      |
| 2    | ProjectType                               | Type of the project                                           | Yes             | `Nuget`, `NPM`, `Poetry`, `Conan`, `Alpine`, `Debian`, `Maven`                         |
| 3    | MultipleProjectType                       | Whether multiple project types are supported                  | No              | `False`                                                                    |
| 4    | Telemetry.Enable                          | Enable telemetry                                              | No              | `False`                                                                    |
| 5    | Telemetry.ApplicationInsightInstrumentKey | Application Insights instrumentation key                      | No              | `123-456-789-123-123`                                                     |
| 6    | SW360.URL                                 | URL of the SW360 server                                       | Yes             | [https://sw360.example.com](https://sw360.example.com)                   |
| 7    | SW360.ProjectName                         | Name of the SW360 project                                     | Yes             | `MyProject`                                                                |
| 8    | SW360.ProjectID                           | ID of the SW360 project                                       | Yes             | `57362e4179ce4e839f286ddf0b91d177`                                         |
| 9    | SW360.AuthTokenType                       | Type of the SW360 token                                       | Yes             | `Bearer` or `Token`                                                          |
| 10   | SW360.Token                               | Auth token for SW360                                          | Yes             | `xxxxxx`                                                                   |
| 11   | SW360.Fossology.URL                       | URL of Fossology server                                       | Yes             | [https://fossology.example.com](https://fossology.example.com)           |
| 12   | SW360.Fossology.EnableTrigger             | Enable Fossology scan trigger                                 | No              | `True`                                                                     |
| 13   | SW360.IgnoreDevDependency                 | Ignore development dependencies                               | No              | `True`                                                                     |
| 14   | SW360.ExcludeComponents                   | Components to exclude (PURL format or ComponentName:Version) | No              | [`"pkg:npm/foobar@12.3.1"`, `"foobar:12.3.1"`, `"foobar:12.*"`, `"foobar:*"`] |
| 15   | Directory.InputFolder                     | Path to input directory                                       | Yes             | `"/mnt/Input"`                                                             |
| 16   | Directory.OutputFolder                    | Path to output directory                                      | Yes             | `"/mnt/Output"`                                                            |
| 17   | Jfrog.URL                                 | URL of JFrog Artifactory                                      | Yes             | [https://jfrog.example.com](https://jfrog.example.com)                 |
| 18   | Jfrog.Token                               | Token for authenticating to JFrog                             | Yes             | `xxxxxx`                                                                   |
| 19   | Jfrog.DryRun                              | Enable dry run (no actual copy/move)                          | No              | `True`                                                                    |

### Below rows repeat for each supported package type.

| S.No | Argument Name                   | Description                           | Is it Mandatory | Example                                   |
| ---- | ------------------------------- | ------------------------------------- | --------------- | ----------------------------------------- |
| 20   | Npm.Include                     | File patterns to include for NPM      | Yes             | `["p*-lock.json", "*.cdx.json"]`         |
| 21   | Npm.Exclude                     | Folders/files to exclude for NPM      | No              | `["node_modules"]`                        |
| 22   | Npm.Artifactory.ThirdPartyRepos | 3rd-party NPM repos and upload toggle | Yes             | `[{"Name": "npm-remote", "Upload": true}]` |
| 23   | Npm.Artifactory.InternalRepos   | Internal NPM repos                    | Yes             | `["npm-internal"]`                         |
| 24   | Npm.Artifactory.DevRepos        | Development NPM repos                 | Yes             | `["npm-dev"]`                              |
| 25   | Npm.Artifactory.RemoteRepos     | Remote NPM repos                      | Yes             | `["npm-remote"]`                           |
| 26   | Npm.ReleaseRepo                 | NPM release repository name           | Yes             | `"npm-release"`                             |
| 27   | Npm.DevDepRepo                  | NPM dev dependency repo name          | Yes             | `"npm-devdep"`                              |

#### **Method 2 - Only Cmd paramaters**
You can also pass the above mentioned arguments in the command line.
Note: If the second approach is followed then make sure you provide all the settings mentioned in the appsettings.json in the command line

#### **Method 3 (Recommended) - AppSettings + Cmd paramaters**
- **Secrets Management**: Sensitive data such as the JFrog token and the SW360 token should be passed as secure variables via command line parameters. This practice ensures that confidential information remains protected.

- **Project Configuration**:
  - Project-specific details such as the project type, SW360 project ID, project name, and directories can be conveniently passed as command line parameters. This allows for flexible and dynamic execution based on project requirements.

- **Application Settings**:
  - For other configuration details, maintain them in an `appSettings` file. You can then pass the path to this settings file using the `--settingsfilepath` command line option. This approach centralizes configuration management, making it easier to track and update.

#### Exclude  Component or Folders :

In order to exclude any components ,it can be configured in the  `appSettings.json` or in the cmdline paramater by providing the `packageName:version` or the `PURL` in the `SW360:ExcludeComponents` field.

- Incase if you want to exclude a single component of the format *"@group/componentname"* eg : @angular/common specify it as *"@group/componentname:version"* i.e @angular/common:4.2.6
- If multiple versions has to be excluded of the same component, specify it as *"@group/componentname:*"* i.e @angular/common:*


In order to **Exclude specific folders** from the execution, It can be specified under the **Exclude section** of that specific **package type**.

### **Continuous Clearing Tool Execution**

Continuous Clearing Tool can be executed as container or as binaries,

  <details>
  <summary>Docker run</summary>

### Prerequisite

1. Install Docker (Latest stable version).
2. Create local directories for mapping to the Continuous clearing tool container directories

   * Input  : Place to keep input files.
   * Output : Resulted files will be stored here.
   * Log    : Continuous clearing log files.
   * CAConfig :  Place to keep Config files i.e., appSettings.json.

**Note** : It is not recommended to use Primary drive(Ex C:) for project execution or directory creation and also the drive should be configured as Shared Drives in docker.

### Package Identifier

* In order to run the PackageIdentifier.dll , execute the below command.

  **Example** : `docker run --rm -it -v /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet PackageIdentifier.dll --settingsfilepath /etc/CATool/appSettings.json`

### SW360 Package Creator

* In order to run the SW360PackageCreator.dll , execute the below command.

  **Example** : `docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet SW360PackageCreator.dll --settingsfilepath /etc/CATool/appSettings.json`

### Artifactory Uploader

* Artifactory uploader is ***not applicable for Debian and Alpine  type package*** clearance.

* In order to run the Artifactory Uploader dll , execute the below command.

  **Example** : `docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSettings.json`

</details>

<details>
<summary>Binary execution</summary>

### Prerequisite

1. .NET 8 runtime [https://dotnet.microsoft.com/download/dotnet-core/8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. Node.js and Git latest

### Package Identifier

* In order to run the PackageIdentifier.exe, execute the below command.

  **Example** : `PackageIdentifier.exe --settingsfilepath /<PathToConfig>/appSettings.json`

### SW360 Package Creator

* In order to run the SW360PackageCreator.exe, execute the below command.

  **Example** : `SW360PackageCreator.exe --settingsfilepath /<PathToConfig>/appSettings.json`

### Artifactory Uploader

* Artifactory uploader is ***not applicable for Debian and Alpine  type package*** clearance.

* In order to run the Artifactory Uploader exe, execute the below command.

  **Example** : `ArtifactoryUploader.exe --settingsfilepath /<PathToConfig>/appSettings.json`

</details>

# Artifactory Uploader Release Execution

By default, the JFrogDryRun is set to True. This configuration is designed for the routine execution of the Artifactory uploader on a daily basis during the project's development phase. The primary objective is to continuously verify the accuracy of component paths and permissions before actual operations.
When the JFrogDryRun is set to False, it indicates a shift towards deployment in a production environment. In this mode, the Artifactory uploader is prepared for live operations, signaling the transition from the verification stage to the actual copy/move of components.

* In order to execute the tool in release mode we need to pass an extra parameter to the existing
  argument list.

  **Example** : docker run --rm -it -v /D/Projects/Output:/mnt/Output -v /D/Projects/DockerLog:/var/log -v /D/Projects/CAConfig:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSettings.json --Jfrog:DryRun false

  or

  **Example** : ArtifactoryUploader.exe --settingsfilepath /<PathToConfig>/appSettings.json -Jfrog:DryRun false

# How to handle multiple project types in same project

Incase your project has both NPM/Nuget or other components it can be handled by merely running then Package Identifier dll multiple times and generating a sinle SBOM.

### Example Steps for Execution:

1. Run the Package Identifier dll with "**ProjectType**" set as "**NPM**".

2. A cycloneDX  BOM will be generated in the output directory path that you have provided.

3. Next run the Package Identifier dll with "**ProjectType**" set as "**NUGET**". In this run make sure that along with the usual arguments you also provide and additional argument "**--MultipleProjectType**" as True.

Note: Do not change the output directories during the multiple runs as the tool automatically picks up the previosly generated SBOM and combines it.

4. Once this is done after the dll run you can find that the components from the first run for "**NPM**" and the components from second run for "**NUGET**" will be merged into one BOM file

5. The remaining steps for the package creator and artifactory uploader remains the same.

# Templates

## Azure DevOps

Sample templates for integrating the Continuous Clearing Tool (CCTool) workflow in Azure Pipelines can be found at templates\azureDevops.
For more details on Azure DevOps templates, refer to the official [Microsoft Documentation](https://learn.microsoft.com/en-us/azure/devops/pipelines/process/templates?view=azure-devops&pivots=templates-includes).

### **Advantages of Running CA Tool via Templates**

* **Simplified Setup:** Avoids adding manual steps for different CCTool stages.
* **Consistency and Standardization:** Ensures uniform execution across the organization.
* **Automated File Uploads:** Handles uploading of logs and BOM files after execution.

---

## Integration

1. **Check-in Templates:** Commit the templates into an Azure DevOps repository.
2. **Reference the Repository:** Include the repository in a new pipeline as shown below:

```yaml
resources:
repositories:
repository: Templates_Pipeline
  type: git
  name: YourProject/Templates_Pipeline
```

:point_right: Note: If the Appsettingsfilepath parameter is not passed, the sample default app settings file is used by the template.

The sample default app settings file is located at templates\sample-default-app-settings.json and can be customized as needed.

### Add a New Template Calling Step
#### NuGet Template Example
```yaml
- template: pipeline/build/pipeline-template-step-install-run-cctool-binary.yml@Templates_Pipeline

  parameters:
    workingDirectory: $(Build.SourcesDirectory)/MyProject
    sw360Token: '$(sw360ApiKey)'
    sw360ProjectId: '$(sw360ProjectID)'
    sw360ProjectName: 'My Project'
    projectDefinitions:
    - projectType: 'nuget'
      inputFolder: $(Build.SourcesDirectory)/src
      exclude: 'Test'
    outputFolder: '$(Build.SourcesDirectory)/output'
    JfrogToken: '$(jfrogToken)'
```

#### Docker Template Example
```yaml
- template: pipeline/build/pipeline-template-step-install-run-cctool-docker.yml@Templates_Pipeline
  parameters:
    workingDirectory: $(Build.SourcesDirectory)/MyProject
    sw360Token: '$(sw360ApiKey)'
    sw360ProjectId: '$(sw360ImagePrjID)'
    sw360ProjectName: 'My Docker Image'
    projectDefinitions:
    - projectType: 'debian'
      inputFolder: $(Build.SourcesDirectory)/images
      imageName: 'myapp'
    outputFolder: '$(Build.SourcesDirectory)/output'
    JfrogToken: '$(jfrogToken)'
```

### Project Definitions Structure

The `projectDefinitions` parameter accepts an array of objects with project-specific configurations:

```yaml

projectDefinitions:
- projectType: 'nuget'               # Mandatory - Project type (nuget, debian, npm, etc.)
  inputFolder: '/path/to/input'      # Mandatory - Folder containing project files
  exclude: 'Test;Temp'               # Optional - semicolon-separated exclusion list

```

For Docker/image-based scanning, additional parameters are available:

```yaml

projectDefinitions:
- projectType: 'debian' # Mandatory - Project type (nuget, debian, npm, etc.)
  inputFolder: '/path/to/input' # Mandatory - Folder containing project files
  imageName: 'debian'                # Mandatory - Only if you want to run against an image on the machine
  imageVersion: 'bookworm-slim'      # Optional  - Version of the Docker image, by default will use the latest image tag

```

:point_right: Please ensure that the image being passed above is present in the machine where the CC tool is being run, as a tar file will be generated based on the image.

## Configuration Parameters
Both templates share common parameters with some implementation-specific differences.

:point_right: Do note that the ones which are not required are values which are retrieved from the default app settings file, if you use your app settings you would need to pass these values, or maintain them in your app settings.

### Common Parameters

| Parameter | Type | Default | Description |Mandatory|
|-----------|------|---------|-------------|---------|
| `sw360Token` | string | '' | SW360 authentication token |:white_check_mark:|
| `sw360ProjectId` | string | '' | Target SW360 project ID  |:white_check_mark:|
| `sw360ProjectName` | string | '' | SW360 project name  |:white_check_mark:|
| `projectDefinitions` | object | [] | List of project configurations to scan  |:white_check_mark:|
| `outputFolder` | string | '' | Output directory for reports and artifacts  |:white_check_mark:|
| `PackageCreatorEnabled` | boolean | true | Enable/disable SW360 package creation |:x:|
| `ArtifactoryUploaderEnabled` | boolean | true | Enable/disable Artifactory uploads |:x:|
| `JfrogToken` | string | '' | JFrog Artifactory authentication token |:white_check_mark:|
| `JfrogDryRun` | boolean | true | Run Artifactory uploads in dry-run mode |:x:|
| `sw360Url` | string | '' | Optional SW360 instance URL |:x:|
| `sw360AuthTokenType` | string | '' | Token type for SW360 (e.g., 'Bearer') |:x:|
| `fossologyUrl` | string | '' | Optional Fossology instance URL |:x:|
| `fossologyEnableTrigger` | boolean | true | Enable/disable Fossology scanning trigger |:x:|
| `JfrogUrl` | string | '' | Optional JFrog Artifactory URL |:x:|
| `enableTelemetry` | boolean | true | Enable/disable telemetry collection |:x:|
| `workingDirectory` | string | '$(Build.SourcesDirectory)' | Base working directory |:x:|
| `excludeComponents` | string | '' | Semicolon-separated list of components to exclude |:x:|
| `appSettingsPath` | string | '' | Your own AppSettings.json path |:x:|


### Binary Template Specific Parameters

| Parameter | Type | Default | Description |Mandatory|
|-----------|------|---------|-------------|---------|
| `toolVersion` | string | '' | Specific version of the binary tool to use |:x:|
| `branchName_powerUser` | string | '' | GitHub branch to build the tool from (for power users) |:x:|

:point_right: Use the `branchName_powerUser` only with prior co-ordination with the Enabler team.

### Docker Template Specific Parameters


| Parameter | Type | Default | Description |Mandatory|
|-----------|------|---------|-------------|---------|
| `branchName_powerUser` | string | '' | GitHub branch to build the Docker image from |:x:|

# Troubleshoot

1. In case your pipeline takes a lot of time to run(more than 1 hour) when there are many components. It is advisable to increase the pipeline timeout and set it to a minimum of 1 hr.

2. In case of any failures in the pipeline, while running the tool,check the following configurations.

   * Make sure your build agents are running.

   * Check if there are any action items to be handled from the user's end.(In this case the exit code with which the pipeline will fail is **2**)

   * Check if the proxy settings environment variables for sw360 is rightly configured in the build machine.

# Manual Update

Upload attachment manually for [Debian](Manual-attachment-Debian-Overview.md) type.

# Bug or Enhancements

For reporting any bug or enhancement and for your feedbacks click [here](https://github.com/siemens/continuous-clearing/issues)

# Glossary of Terms

| **3P Components** | **3rd Party Components**  |
| ----------------- | ------------------------- |
| BOM               | Bill of Material          |
| apiAuthToken      | SW360 authorization token |

# References

## Image References

* Fetching Project Id from SW360

![sw360pic](../usagedocimg/sw360.PNG)

## API References

* SW360 API Guide : [https://www.eclipse.org/sw360/docs/development/restapi/dev-rest-api/](https://www.eclipse.org/sw360/docs/development/restapi/dev-rest-api/)
* FOSSology API Guide: [https://www.fossology.org/get-started/basic-rest-api-calls/](https://www.fossology.org/get-started/basic-rest-api-calls/)

Copyright © Siemens AG ▪ 2025
