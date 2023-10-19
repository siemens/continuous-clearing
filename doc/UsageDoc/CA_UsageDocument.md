# Continuous Clearing Tool

## Contents
<!--ts-->

* [Introduction](#introduction)

* [Continuous Clearing Tool workflow diagram](#continuous-clearing-tool-workflow-diagram)

* [Prerequisite](#prerequisite)

* [Package Installation](#installation) 

* [Demo Project](#demo-project-after-consuming-the-package)

* [Continuous Clearing Tool Execution](#continuous-clearing-tool-execution)

  * [Overview](#overview)

  * [Prerequisite for execution](#prerequisite-for-continuous-clearing-tool-execution) 

  * [Configuration](#configuring-the-continuous-clearing-tool)

  * [Execution](#continuous-clearing-tool-execution)
  

 * [Continuous Clearing Tool Execution Test Mode](#continuous-clearing-tool-execution-test-mode)

* [How to handle multiple project types in same project](#how-to-handle-multiple-project-types-in-same-project)

* [Troubleshoot](#troubleshoot)

* [Manual Update](#manual-update)

* [Bug or Enhancements](#bug-or-enhancements)

* [Glossary of Terms](#glossary-of-terms)

* [References](#references)

  * [Image References](#image-references)

  * [API References](#api-references)


<!--te-->
# Introduction

The Continuous Clearing Tool helps the Project Manager/Developer to automate the sw360 clearing process of 3rd party components. This tool scans and identifies the third-party components used in a NPM, NUGET, MAVEN,PYTHON and Debian  projects and makes an entry in SW360, if it is not present. Continuous Clearing Tool links the components to the respective project and creates job for code scan in FOSSology.The output is an SBOM file which has a nested description of software artifact components and metadata.

Continuous Clearing Tool reduces the effort in creating components in SW360 and identifying the matching source codes from the public repository. Tool eliminates the manual error while creating component and identifying correct version of source code from public repository. Continuous Clearing Tool harmonize the creation of 3P components in SW360 by filling necessary information.

# Continuous Clearing Tool workflow diagram

- Package Identifier
   - [NPM/NUGET/MAVEN/PYTHON](../usagedocimg/packageIdentifiernpmnuget.PNG)
   - [Debian](../usagedocimg/packageIdentifierdebian.PNG)
- SW360 Package Creator
  - [NPM/NUGET/MAVEN/PYTHON](../usagedocimg/packageCreatirnpmnuget.PNG)
  - [Debian](../usagedocimg/packagecreatordebian.PNG)
- Artifactory Uploader
  - [NPM/NUGET/MAVEN/PYTHON](../usagedocimg/artifactoryuploader.PNG)
 
# Prerequisite

1. **Make an entry for your project in SW360** for license clearance and is **should be in Active state** while running Continuous Clearing Tool


2. **Access request :**

   Get SW360 REST API Authentication token

   > **_1.SW360 token :_**    
   
   > >a) The user can generate a token from their functional account. 

   > >b) The necessary credentials for token generation i.e the client id and client secret.

   >**_2.Artifactory Token :_**

   >>a)For enabling the upload of cleared packages into jfrog artifactory, user's need to have their own jfrog artifactory credentials.This includes a username and an Apikey.

   **Pipeline Configuration :**

   For certain  scenarios we have predefined exit codes as mentioned below:

   |Exit Code| Scenario |
   |--|--|
   | 0 | Success |
   | 1 | Critical failure/error in the run |
   | 2 | Action item required from user's side |

   While configuring the Continuous Clearing Tool in the pipeline , user can configure each stage to display the result based on these exit codes. 
   
   This can be done by the configuration management team at the time of modifying the pipeline to support Continuous Clearing Tool.
  
   After the configuration your pipeline will look like this : 
   
   ![folderpic](../usagedocimg/piplinepic.PNG)

# Installation   
  ### Use container image 

    docker pull ghcr.io/siemens/continuous-clearing:latest

 ### Nuget package
  
   Download the [.nupkg](https://github.com/siemens/continuous-clearing/releases) file from GitHub releases. 


# Demo project after consuming the package 
 You can find sample yml files under the [DemoProject](../../DemoProject).

# Continuous Clearing Tool Execution

 ### Overview
 
 The Continuous Clearing Tool has 3 dlls, Execute them in the following order to achieve the complete License clearing process.
 
    
   > **1. Package Identifier**
      - Processes the input file and generates CycloneDX BOM file. The input file can be package file or a cycloneDx BOM file generated using the standard tool. If there are multiple input files, it can be processed by just passing the path to the directory in the argument


   >**2. SW360 Package Creator**
      - Process the SBOM file(i.e., output of the first dll) and creates the missing components/releases in SW360 and links all the components to the project in the SW360 portal. This exe also triggers the upload of the components to Fossology and automatically updates the clearing state in SW360.

      `Note : Since the PackageIdentifier generates an SBOM file both Dev dependency and internal components will be existing in the BOM file.Make sure to set `RemoveDevDependency` Flag as true while running this exe`
	  
   >**3. Artifactory Uploader**
      - Processes the CycloneDXBOM file(i.e., the output of the SW360PackageCreator) and uploads the already cleared components(clearing state-Report approved) to the siparty release repo in Jfrog Artifactory.The components in the states other than "Report approved" will be handled by the clearing experts via the Continuous Clearing Dashboard.

### **Prerequisite for Continuous Clearing Tool execution** 

   - Input files according to project type

      - **Project Type :** **NPM** 

          * Input file repository should contain **package-lock.json** file. If not present do an `npm install`.
          ![folderpic](../usagedocimg/npminstall.PNG)
      
      - **Project Type :** **Nuget**
      
          * .Net core/.Net standard type project's input file repository should contain **package.lock.json** file. If not present do a `dotnet restore --use-lock-file`.
          
          * .Net Framework projects, input file repository should contain a **packages.config** file.

      - **Project Type :** **Maven**
      
          * [Apache Maven](https://dlcdn.apache.org/maven/maven-3/3.9.0/binaries/apache-maven-3.9.0-bin.zip) has to be installed in the build machine and added in the `PATH` variable.
		  *Add the cycloneDX Maven Plugin to the main **pom.xml" and run the command to generate the input bom file.
		  
				 mvn install cyclonedx:makeAggregateBom

          * Input file repository should contain **bom.cdx.json** file,Which will be the output of CycloneDx-Maven-Plugin tool

         * **Note** : Incase your project has internal dependencies, compile the project **prior to running the clearing tool**
 
                 mvn clean install -DskipTests=true 

       - **Project Type :** **Python** 

          * Input file repository should contain **poetry.lock** file. 
		 
     - **Project Type :**  **Debian** 
       
   	      **Note** : below steps is required only if you have `tar` file to process , otherwise you can keep `CycloneDx.json` file in the InputDirectory.
          *  Create `InputImage` directory for keeping `tar` images and `InputDirectory` for resulted file storing .

          *  Run the command given below by replacing the place holder values (i.e., path to input image directory, path to input directory and file name of the Debian image to be cleared) with actual values.
            
              **Example**:   `docker run --rm -v <path/to/InputImageDirectory>:/tmp/InputImages -v <path/to/InputDirectory>:/tmp/OutputFiles ghcr.io/siemens/continuous-clearing ./syft packages /tmp/InputImages/<fileNameofthedebianImageTobeCleared.tar> -o cyclonedx-json --file "/tmp/OutputFiles/output.json"`
           
           
             After successful execution, `output.json` (_CycloneDX.json_) file will be created in specified directory
           
             ![image.png](../usagedocimg/output.PNG)
           
             Resulted `output.json` file will be having the list of installed packages  and the same file will be used as  an input to `Continuous clearing tool - Bom creator` as an argument(`--packagefilepath`). The remaining process is same as other project types.


### **Configuring the Continuous Clearing Tool**

   Arguments can be provided to the tool in two ways :

 #### **Method 1 (Recommended)**
   Copy the below content and create new `appSettings.json` file in `Continuous Clearing tool Config` directory.
   
   Below is the list of settings can be made in `appSettings.json` file.

   _`Sample appSettings.json file`_

 
```
{
  "CaVersion": "4.0.0",
  "TimeOut": 200,
  "ProjectType": "<Insert ProjectType>",
  "SW360ProjectName": "<Insert SW360 Project Name>",
  "SW360ProjectID": "<Insert SW360 Project Id>",
  "Sw360AuthTokenType": "Bearer",
  "Sw360Token": "<Insert SW360Token in a secure way>",
  "SW360URL": "<Insert SW360URL>",
  "Fossologyurl": "<Insert Fossologyurl>",
  "JFrogApi": "<Insert JFrogApi>",
  "JfrogNugetDestRepoName": "JfrogNugetDestRepo Name",
  "JfrogNpmDestRepoName": "JfrogNpmDestRepo Name",
  "JfrogMavenDestRepoName": "JfrogMavenDestRepo Name",
  "JfrogPythonDestRepoName": "JfrogPythonDestRepo Name",
  "PackageFilePath": "/mnt/Input",
  "BomFolderPath": "/mnt/Output",
  "BomFilePath":"/mnt/Output/<SW360 Project Name>_Bom.cdx.json",
//IdentifierBomFilePath : For multiple project type 
  "IdentifierBomFilePath": "",
//CycloneDxSBomTemplatePath : To be used when customer is providing manual SBOM template
  "CycloneDxSBomTemplatePath": "/PathToSBOMTemplateFile",
  "ArtifactoryUploadApiKey": "<Insert ArtifactoryUploadApiKey in a secure way>",//This should be Jfrog Key
  "ArtifactoryUploadUser": "<Insert ArtifactoryUploadUser>",//This should be Jfrog user name
  "RemoveDevDependency": true,
  "EnableFossTrigger": true,
  "InternalRepoList": [
    "<Npm Internal Repo Names>", //This should be the internal repo names in JFrog for NPM
    "<Nuget Internal Repo Names>",//This should be the internal repo names in JFrog for Nuget
    "<Maven Internal Repo Names>",//This should be the internal repo names in JFrog for Maven
    "<Python Internal Repo Names>",//This should be the internal repo names in JFrog for Python
  ],
  "Npm": {
    "Include": [ "p*-lock.json" ,"*.cdx.json"],
    "Exclude": [ "node_modules" ],
    "JfrogNpmRepoList": [
      "<Npm Remote Cache Repo Name>",//This is a mirror repo for npm registry in JFrog
      "<Npm Release Repo Name>", //This should be the release repo in JFrog
    ],
    "ExcludedComponents": []
  },
  "Nuget": {
    "Include": [ "pack*.config", "p*.assets.json", "*.cdx.json" ],
    "Exclude": [],
    "JfrogNugetRepoList": [
      "<Nuget Remote Cache Repo Name>",//This is a mirror repo for nuget.org in JFrog
      "<Nuget Release Repo Name>",//This should be the release repo in JFrog
    ],
    "ExcludedComponents": []
  },
  "Maven": {
    "Include": [ "*.cdx.json" ],
    "Exclude": [],
    "JfrogMavenRepoList": [
      "<Maven Remote Cache Repo Name>",//This is a mirror repo for repo.maven in JFrog
      "<Maven Release Repo Name>",//This should be the release repo.maven in JFrog
    ],
    "ExcludedComponents": []
  },
  "Debian": {
    "Include": [ "*.json" ],
    "Exclude": [],
    "ExcludedComponents": []
  },
  "Python": {
    "Include": [ "poetry.lock", "*.cdx.json" ],
    "Exclude": [],
    "JfrogPythonRepoList": [
      <Python Remote Cache Repo Name>, //This is a mirror repo for pypi in JFrog
      "<Python Release Repo Name>" //This should be the release pypi in JFrog
    ],
    "ExcludedComponents": []
  }
}
```

Description for the settings in `appSettings.json` file

|S.No| Argument name   |Description  | Is it Mandatory    | Example |
|--|--|--|--|--|
| 1 |--packagefilepath   | Path to the package-lock.json file or to the directory where the project is present in case we have multiple package-lock.json files.                                      |Yes ,For Docker run /mnt/Input | D:\Clearing Automation |
| 2 |--cylonedxsbomtemplatepath | Path to the SBOM cycloneDx BOM file. Can be passed along with packagefilepath.                           |No if the first argument is provided| D:\ExternalToolOutput|
| 3 |--bomfolderpath | Path to keep the generated boms  |  Yes , For Docker run /mnt/Output    | D:\Clearing Automation\BOM
|  4| --sw360token  |  SW360 Auth Token |  Yes| Refer the SW360 Doc [here](https://www.eclipse.org/sw360/docs/development/restapi/access).Make sure you pass this credential in a secured way. |
| 5 | --sw360projectid |  Project ID from SW360 project URL of the project  |  Yes| Obtained from SW360 |
|  6|  --projecttype    | Type of the package         | Yes |  NPM/NUGET/Debian/MAVEN |
|7 | --removedevdependency  |  Make this field to `true` , if Dev dependencies needs to be excluded from clearing |  Optional ( By default set to true) | true/false |
| 8|  --sw360url  |  SW360 URL              |Yes |  https://<my_sw360_server>|
|  9| --sw360authtokentype   |  SW360 Auth Token  |Yes  | Token/Bearer |
|10  |  --settingsfilepath |  appSettings.json file path                                                                                                                             |Optional (By default it will take from the  bom creator exe location     |  |
|  11|  --artifactoryuploadapikey  | JFrog Auth Token          |  Yes| Generated from Jfrog Artifactory.Make sure you pass this credential in a secured way. |
|  12|  --bomfilepath  | CycloneDX BOM Filepath (output generated from the previous Package Identifier run) i.e The file path of the *_Bom.cdx.json file         |  Yes| For SW360PackageCreator & ArtifactoryUploader run needs to provide this path. |
|  13|  --identifierbomfilepath  | CycloneDX BOM Filepath (output generated from the previous Package Identifier run,applicable only if there are multiple project types) i.e The file path of the *_Bom.cdx.json file         |  No| If there are multiple project type this argument can be used. |
|  14|  --logfolderpath | Path to create log        |  No| If user wants to give configurable log path this parameter is used |
| 15   | --fossologyurl | Fossology URL                                                                                                                                        | Yes |      https://<my_fossology_server>                                                                                                                     | Yes                      |                                                                                                                          | Optional (By default it will take from the  Package Creator exe location     |                                                    |
| 16    | --artifactoryuploaduser              | Jfrog User Email                              | Yes                                                       |
| 17  | --jfrognpmdestreponame         | The destination folder name for the NPM package to be copied to                  | Yes                                                    |
| 18    | --jfrognugetdestreponame         | The destination folder name for the Nuget package to be copied to                  | Yes                                                    |
| 19    | --jfrogmavendestreponame         | The destination folder name for the Maven package to be copied to                  | Yes                                                    |                                            |
| 20    | --jfrogpythondestreponame         | The destination folder name for the Python package to be copied to                  | Yes                                                    |                                            |
| 21   | --timeout          | SW360 response timeout value                  | No                                                       |                                                |


 #### **Method 2**

You can also pass the above mentioned arguments in the command line.

`Note: If the second approach is followed then make sure you provide all the settings mentioned in the appsettings.json in the command line`
  
  #### Exclude  Component or Folders :
  In order to exclude any components ,it can be configured in the  "appSettings.json" by providing the package name and version as specified above in the *_ExcludedComponents_* field.
  
  Incase if you want to exclude a single component of the format _"@group/componentname"_ `eg : @angular/common` specify it as _"@group/componentname:version"_ i.e `@angular/common:4.2.6`

  If multiple versions has to be excluded of the same component specify it as _"@group/componentname:*"_ i.e `@angular/common:*`
  
  In order to **Exclude specific folders** from the execution, It can be specified under the **Exclude section** of that specific **package type**.


### **Continuous Clearing Tool Execution** 

Continuous Clearing Tool can be executed as container or as binaries,

  <details>
  <summary>Docker run</summary>

   ### Prerequisite
   1. Install Docker (Latest stable version).
   2.  Create local directories for mapping to the Continuous clearing tool container directories
        - Input  : Place to keep input files.
        - Output : Resulted files will be stored here.
        - Log    : Continuous clearing log files.
        - CAConfig :  Place to keep Config files i.e., `appSettings.json`.



  **Note** : It is not recommended to use `Primary drive(Ex C:\)` for project execution or directory creation and also the `drive` should be configured as `Shared Drives` in docker.

### Package Identifier

  - In order to run the PackageIdentifier.dll , execute the below command.

    **Example** : `docker run --rm -it -v /path/to/InputDirectory:/mnt/Input -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet PackageIdentifier.dll --settingsfilepath /etc/CATool/appSettings.json`


### SW360 Package Creator

  - In order to run the SW360PackageCreator.dll , execute the below command. 

    **Example** : `docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet SW360PackageCreator.dll --settingsfilepath /etc/CATool/appSettings.json`

###  Artifactory Uploader

  * Artifactory uploader is **_`not applicable for Debian type package`_** clearance.

  *  In order to run the Artifactory Uploader dll , execute the below command.
  
     **Example** : `docker run --rm -it -v /path/to/OutputDirectory:/mnt/Output -v /path/to/LogDirectory:/var/log -v /path/to/configDirectory:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSettings.json`

</details>

<details>
<summary>Binary execution</summary>

### Prerequisite
1. .NET 6 runtime [https://dotnet.microsoft.com/download/dotnet-core/6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
2. Node.js and Git latest


 ### Package Identifier

  - In order to run the PackageIdentifier.exe, execute the below command.

    **Example** : `PackageIdentifier.exe --settingsfilepath /<PathToConfig>/appSettings.json`

### SW360 Package Creator

  - In order to run the SW360PackageCreator.exe, execute the below command. 

    **Example** : `SW360PackageCreator.exe --settingsfilepath /<PathToConfig>/appSettings.json`

###  Artifactory Uploader

  * Artifactory uploader is **_`not applicable for Debian type package`_** clearance.

  *  In order to run the Artifactory Uploader exe, execute the below command.
  
     **Example** : `ArtifactoryUploader.exe --settingsfilepath /<PathToConfig>/appSettings.json`
</details>

# Continuous Clearing Tool Execution Test Mode

  The purpose the test mode execution of the tool is to ensure that there are no any connectivity issues with SW360 server.
  
  - In order to execute the tool in test mode we need to pass an extra parameter to the existing 
argument list.
    
    **Example** : `docker run --rm -it -v /D/Projects/Output:/mnt/Output -v /D/Projects/DockerLog:/var/log -v /D/Projects/CAConfig:/etc/CATool ghcr.io/siemens/continuous-clearing dotnet ArtifactoryUploader.dll --settingsfilepath /etc/CATool/appSettings.json --mode test`

    or

    **Example** : `ArtifactoryUploader.exe --settingsfilepath /<PathToConfig>/appSettings.json --mode test`


# How to handle multiple project types in same project

Incase your project has both NPM/Nuget components it can be handled by merely running then `Package Identifier dll` twice.
### Steps for Execution:
1. Run the `Package Identifier dll` with "**ProjectType**" set as "**NPM**" in `appSettings.json` .

2. A cycloneDX  BOM will be generated in the output BOM path that you provide in the argument.
3. Next run the `Package Identifier dll` with "**ProjectType**" set as "**NUGET**". In this run make sure that along with the usual arguments you also provide and additional argument "**--identifierBomFilePath**" which will contain the comparison BOM file path which is generated in the previous run.

4. Once this is done after the dll run you can find that the components from the first run for "**NPM**" and the components from second run for "**NUGET**" will be merged into one BOM file



# Troubleshoot
1. In case your pipeline takes a lot of time to run(more than 1 hour) when there are many components. It is advisable to increase the pipeline timeout and set it to a minimum of 1 hr.

1. In case of any failures in the pipeline, while running the tool,check the following configurations.
   * Make sure your build agents are running.
   * Check if there are any action items to be handled from the user's end.(In this case the exit code with which the pipeline will fail is **2**)

   * Check if the proxy settings environment variables for sw360 is rightly configured in the build machine.


# Manual Update
Upload attachment manually for [Debian](Manual-attachment-Debian-Overview.md) type.


# Bug or Enhancements

For reporting any bug or enhancement and for your feedbacks click [here](https://github.com/siemens/continuous-clearing/issues)
 

# Glossary of Terms

| **3P Components** | **3rd Party Components**  |
|-------------------|---------------------------|
| BOM               | Bill of Material          |
| apiAuthToken      | SW360 authorization token |

# References
 ## Image References
- Fetching Project Id from SW360

![sw360pic](../usagedocimg/sw360.PNG)


## API References 

- SW360 API Guide : [https://www.eclipse.org/sw360/docs/development/restapi/dev-rest-api/](https://www.eclipse.org/sw360/docs/development/restapi/dev-rest-api/)
- FOSSology API Guide: [https://www.fossology.org/get-started/basic-rest-api-calls/](https://www.fossology.org/get-started/basic-rest-api-calls/)

Copyright © Siemens AG ▪ 2023
