// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Logging;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace LCT.ArtifactoryUploader
{
    public static class PackageUploadInformation
    {
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string ReportFileName = "Artifactory";

        #endregion

        #region Methods

        /// <summary>
        /// Gets the components to be packaged with initialized display information.
        /// </summary>
        /// <returns>A DisplayPackagesInfo object with initialized package lists.</returns>
        public static DisplayPackagesInfo GetComponentsToBePackages()
        {
            DisplayPackagesInfo displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.UnknownPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesChoco = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesChoco = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesChoco = new List<ComponentsToArtifactory>();


            return displayPackagesInfo;

        }

        /// <summary>
        /// Gets the uploaded package details from the display packages information.
        /// </summary>
        /// <param name="displayPackagesInfo">The display information containing all package lists.</param>
        /// <returns>A list of successfully uploaded components.</returns>
        public static List<ComponentsToArtifactory> GetUploadePackageDetails(DisplayPackagesInfo displayPackagesInfo)
        {
            List<ComponentsToArtifactory> uploadedPackages = new List<ComponentsToArtifactory>();

            // Use a helper method to process each package type
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesConan);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesMaven);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesNpm);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesNuget);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesPython);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesDebian);
            AddSuccessfulPackages(uploadedPackages, displayPackagesInfo.JfrogFoundPackagesCargo);

            return uploadedPackages;
        }

        /// <summary>
        /// Helper method to add packages with successful status codes to the uploaded packages list
        /// </summary>
        /// <param name="uploadedPackages">The list to add successful packages to</param>
        /// <param name="packages">The packages to check</param>
        private static void AddSuccessfulPackages(List<ComponentsToArtifactory> uploadedPackages, List<ComponentsToArtifactory> packages)
        {
            if (packages == null) return;

            foreach (var item in packages)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }
        }

        /// <summary>
        /// Displays package upload information for all package types.
        /// </summary>
        /// <param name="displayPackagesInfo">The display information containing all package lists.</param>
        public static void DisplayPackageUploadInformation(DisplayPackagesInfo displayPackagesInfo)
        {
            string localPathforartifactory = ArtifactoryUploader.GettPathForArtifactoryUpload();

            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNpm, displayPackagesInfo.JfrogNotFoundPackagesNpm, displayPackagesInfo.SuccessfullPackagesNpm, displayPackagesInfo.JfrogFoundPackagesNpm, "npm", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNuget, displayPackagesInfo.JfrogNotFoundPackagesNuget, displayPackagesInfo.SuccessfullPackagesNuget, displayPackagesInfo.JfrogFoundPackagesNuget, "NuGet", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesMaven, displayPackagesInfo.JfrogNotFoundPackagesMaven, displayPackagesInfo.SuccessfullPackagesMaven, displayPackagesInfo.JfrogFoundPackagesMaven, "Maven", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesConan, displayPackagesInfo.JfrogNotFoundPackagesConan, displayPackagesInfo.SuccessfullPackagesConan, displayPackagesInfo.JfrogFoundPackagesConan, "Conan", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesPython, displayPackagesInfo.JfrogNotFoundPackagesPython, displayPackagesInfo.SuccessfullPackagesPython, displayPackagesInfo.JfrogFoundPackagesPython, "Poetry", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesDebian, displayPackagesInfo.JfrogNotFoundPackagesDebian, displayPackagesInfo.SuccessfullPackagesDebian, displayPackagesInfo.JfrogFoundPackagesDebian, "Debian", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesCargo, displayPackagesInfo.JfrogNotFoundPackagesCargo, displayPackagesInfo.SuccessfullPackagesCargo, displayPackagesInfo.JfrogFoundPackagesCargo, "Cargo", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesChoco, displayPackagesInfo.JfrogNotFoundPackagesChoco, displayPackagesInfo.SuccessfullPackagesChoco, displayPackagesInfo.JfrogFoundPackagesChoco, "Choco", localPathforartifactory);

        }

        /// <summary>
        /// Displays sorted components for each package type based on their status.
        /// </summary>
        /// <param name="unknownPackages">List of unknown packages.</param>
        /// <param name="JfrogNotFoundPackages">List of packages not found in JFrog.</param>
        /// <param name="SucessfullPackages">List of successfully processed packages.</param>
        /// <param name="JfrogFoundPackages">List of packages found in JFrog.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filePath">The file path for storing package information.</param>
        private static void DisplaySortedForeachComponents(
    List<ComponentsToArtifactory> unknownPackages,
    List<ComponentsToArtifactory> JfrogNotFoundPackages,
    List<ComponentsToArtifactory> SucessfullPackages,
    List<ComponentsToArtifactory> JfrogFoundPackages,
    string name,
    string filePath)
        {
            if (!HasAnyPackages(unknownPackages, JfrogNotFoundPackages, SucessfullPackages, JfrogFoundPackages))
            {
                return;
            }

            if (LoggerFactory.UseSpectreConsole)
            {
                DisplayWithSpectreConsole(unknownPackages, JfrogNotFoundPackages, SucessfullPackages, JfrogFoundPackages, name, filePath);
            }
            else
            {
                DisplayWithLogger(unknownPackages, JfrogNotFoundPackages, SucessfullPackages, JfrogFoundPackages, name, filePath);
            }
        }

        /// <summary>
        /// Checks if any of the package lists contain packages.
        /// </summary>
        /// <param name="packageLists">Variable number of package lists to check.</param>
        /// <returns>True if any list contains packages, otherwise false.</returns>
        private static bool HasAnyPackages(params List<ComponentsToArtifactory>[] packageLists)
        {
            return packageLists.Any(list => list?.Count > 0);
        }

        /// <summary>
        /// Displays package information using Spectre Console formatting.
        /// </summary>
        /// <param name="unknownPackages">List of unknown packages.</param>
        /// <param name="JfrogNotFoundPackages">List of packages not found in JFrog.</param>
        /// <param name="SucessfullPackages">List of successfully processed packages.</param>
        /// <param name="JfrogFoundPackages">List of packages found in JFrog.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        private static void DisplayWithSpectreConsole(
            List<ComponentsToArtifactory> unknownPackages,
            List<ComponentsToArtifactory> JfrogNotFoundPackages,
            List<ComponentsToArtifactory> SucessfullPackages,
            List<ComponentsToArtifactory> JfrogFoundPackages,
            string name, string filepath)
        {
            LoggerHelper.SafeSpectreAction(() =>
            {
                var content = new StringBuilder($"[green]{name}[/]\n\n");
                AppendPackageContent(content, unknownPackages, JfrogFoundPackages, JfrogNotFoundPackages, SucessfullPackages, name, filepath);

                LoggerHelper.WriteStyledPanel(content.ToString().TrimEnd(), "", "blue", "yellow");
                LoggerHelper.WriteLine();
            }, $"Display {name} Package Information", "Info");
        }

        /// <summary>
        /// Appends package content to a StringBuilder for display.
        /// </summary>
        /// <param name="content">The StringBuilder to append content to.</param>
        /// <param name="unknownPackages">List of unknown packages.</param>
        /// <param name="JfrogFoundPackages">List of packages found in JFrog.</param>
        /// <param name="JfrogNotFoundPackages">List of packages not found in JFrog.</param>
        /// <param name="SucessfullPackages">List of successfully processed packages.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filePath">The file path for storing package information.</param>
        private static void AppendPackageContent(
            StringBuilder content,
            List<ComponentsToArtifactory> unknownPackages,
            List<ComponentsToArtifactory> JfrogFoundPackages,
            List<ComponentsToArtifactory> JfrogNotFoundPackages,
            List<ComponentsToArtifactory> SucessfullPackages, string name, string filePath)
        {
            AppendUnknownPackages(content, unknownPackages, name, filePath);
            AppendJfrogFoundPackages(content, JfrogFoundPackages);
            AppendJfrogNotFoundPackages(content, JfrogNotFoundPackages);
            AppendSuccessfulPackages(content, SucessfullPackages);
        }

        /// <summary>
        /// Appends unknown packages information to the content.
        /// </summary>
        /// <param name="content">The StringBuilder to append content to.</param>
        /// <param name="packages">List of unknown packages.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        private static void AppendUnknownPackages(StringBuilder content, List<ComponentsToArtifactory> packages, string name, string filepath)
        {
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");
            if (packages?.Count > 0)
            {
                content.AppendLine($"[yellow]Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}[/]\n");
                DisplayErrorForUnknownPackages(packages, name, filepath);
            }
        }

        /// <summary>
        /// Appends JFrog found packages information to the content.
        /// </summary>
        /// <param name="content">The StringBuilder to append content to.</param>
        /// <param name="packages">List of packages found in JFrog.</param>
        private static void AppendJfrogFoundPackages(StringBuilder content, List<ComponentsToArtifactory> packages)
        {
            if (packages?.Count > 0)
            {
                content.AppendLine();
                foreach (var package in packages)
                {
                    content.AppendLine(FormatJfrogFoundPackage(package));
                }
                content.AppendLine();
            }
        }

        /// <summary>
        /// Formats a JFrog found package for display.
        /// </summary>
        /// <param name="package">The package to format.</param>
        /// <returns>A formatted string representation of the package.</returns>
        private static string FormatJfrogFoundPackage(ComponentsToArtifactory package)
        {
            if (package.ResponseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
            {
                return $"❌ [white]{package.Name}[/]-[cyan]{package.Version}[/] " +
                       $"[red]{package.OperationType} Failed![/] " +
                       $"[yellow]{package.SrcRepoName}[/] [white]⟶ [/] [yellow]{package.DestRepoName}[/]";
            }

            if (package.ResponseMessage.ReasonPhrase == ApiConstant.PackageNotFound)
            {
                return $"❌ Package [white]{package.Name}[/]-[cyan]{package.Version}[/] not found in [yellow]{package.SrcRepoName}[/],[red] Upload Failed!![/]";
            }

            return $"✓ [green]Successful{package.DryRunSuffix}[/] " +
                   $"[cyan]{package.OperationType}[/] " +
                   $"[white]{package.Name}[/]-[cyan]{package.Version}[/] " +
                   $"from [yellow]{package.SrcRepoName}[/] [white]⟶ [/] [yellow]{package.DestRepoName}[/]";
        }

        /// <summary>
        /// Appends JFrog not found packages information to the content.
        /// </summary>
        /// <param name="content">The StringBuilder to append content to.</param>
        /// <param name="packages">List of packages not found in JFrog.</param>
        private static void AppendJfrogNotFoundPackages(StringBuilder content, List<ComponentsToArtifactory> packages)
        {
            if (packages?.Count > 0)
            {
                content.AppendLine();
                foreach (var package in packages)
                {
                    content.AppendLine($"⚠ [white]{package.Name}[/]-[cyan]{package.Version}[/] [yellow]is not found in jfrog[/]");
                }
                content.AppendLine();
            }
        }

        /// <summary>
        /// Appends successful packages information to the content.
        /// </summary>
        /// <param name="content">The StringBuilder to append content to.</param>
        /// <param name="packages">List of successfully processed packages.</param>
        private static void AppendSuccessfulPackages(StringBuilder content, List<ComponentsToArtifactory> packages)
        {
            if (packages?.Count > 0)
            {
                content.AppendLine();
                foreach (var package in packages)
                {
                    content.AppendLine($"✓ [white]{package.Name}[/]-[cyan]{package.Version}[/] [green]is already uploaded[/]");
                }
            }
        }

        /// <summary>
        /// Displays package information using standard logger.
        /// </summary>
        /// <param name="unknownPackages">List of unknown packages.</param>
        /// <param name="JfrogNotFoundPackages">List of packages not found in JFrog.</param>
        /// <param name="SucessfullPackages">List of successfully processed packages.</param>
        /// <param name="JfrogFoundPackages">List of packages found in JFrog.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filePath">The file path for storing package information.</param>
        private static void DisplayWithLogger(
            List<ComponentsToArtifactory> unknownPackages,
            List<ComponentsToArtifactory> JfrogNotFoundPackages,
            List<ComponentsToArtifactory> SucessfullPackages,
            List<ComponentsToArtifactory> JfrogFoundPackages,
            string name,
            string filePath)
        {
            Logger.Info($"\n{name}:\n");
            DisplayErrorForUnknownPackages(unknownPackages, name, filePath);
            DisplayErrorForJfrogFoundPackages(JfrogFoundPackages);
            DisplayErrorForJfrogPackages(JfrogNotFoundPackages);
            DisplayErrorForSucessfullPackages(SucessfullPackages);
        }

        /// <summary>
        /// Displays error information for packages found in JFrog.
        /// </summary>
        /// <param name="JfrogFoundPackages">List of packages found in JFrog.</param>
        public static void DisplayErrorForJfrogFoundPackages(List<ComponentsToArtifactory> JfrogFoundPackages)
        {

            if (JfrogFoundPackages.Count != 0)
            {

                foreach (var jfrogFoundPackage in JfrogFoundPackages)
                {

                    if (jfrogFoundPackage.ResponseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
                    {
                        Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} {jfrogFoundPackage.OperationType} Failed!! {jfrogFoundPackage.SrcRepoName} ---> {jfrogFoundPackage.DestRepoName}");
                    }
                    else if (jfrogFoundPackage.ResponseMessage.ReasonPhrase == ApiConstant.PackageNotFound)
                    {
                        Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} not found in {jfrogFoundPackage.SrcRepoName}, Upload Failed!!");
                    }
                    else
                    {
                        Logger.Info($"Successful{jfrogFoundPackage.DryRunSuffix} {jfrogFoundPackage.OperationType} package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version}" +
                                          $" from {jfrogFoundPackage.SrcRepoName} to {jfrogFoundPackage.DestRepoName}");
                    }

                }
                Logger.Info("\n");

            }
        }

        /// <summary>
        /// Displays error information for packages not found in JFrog.
        /// </summary>
        /// <param name="JfrogNotFoundPackages">List of packages not found in JFrog.</param>
        public static void DisplayErrorForJfrogPackages(List<ComponentsToArtifactory> JfrogNotFoundPackages)
        {

            if (JfrogNotFoundPackages.Count != 0)
            {

                foreach (var jfrogNotFoundPackage in JfrogNotFoundPackages)
                {
                    Logger.Warn($"Package {jfrogNotFoundPackage.Name}-{jfrogNotFoundPackage.Version} is not found in jfrog");

                }
                Logger.Info("\n");

            }
        }

        /// <summary>
        /// Displays information for successfully processed packages.
        /// </summary>
        /// <param name="SucessfullPackages">List of successfully processed packages.</param>
        private static void DisplayErrorForSucessfullPackages(List<ComponentsToArtifactory> SucessfullPackages)
        {

            if (SucessfullPackages.Count != 0)
            {

                foreach (var sucessfullPackage in SucessfullPackages)
                {
                    Logger.Info($"Package {sucessfullPackage.Name}-{sucessfullPackage.Version} is already uploaded");
                }
                Logger.Info("\n");

            }
        }

        /// <summary>
        /// Displays a warning message when there are no packages to upload.
        /// </summary>
        /// <param name="filename">The filename where package details can be found.</param>
        private static void WarningMessageForNoPackages(string filename)
        {
            if (!LoggerFactory.UseSpectreConsole)
                Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }

        /// <summary>
        /// Displays error information for unknown packages.
        /// </summary>
        /// <param name="unknownPackages">List of unknown packages.</param>
        /// <param name="name">The name of the package type.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        private static void DisplayErrorForUnknownPackages(List<ComponentsToArtifactory> unknownPackages, string name, string filepath)
        {
            ProjectResponse projectResponse = new ProjectResponse();
            IFileOperations fileOperations = new FileOperations();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            if (unknownPackages.Count != 0)
            {
                var packageHandlers = new Dictionary<string, Action<List<ComponentsToArtifactory>, ProjectResponse, IFileOperations, string, string>>
        {
            { "Npm", GetNotApprovedNpmPackages },
            { "Nuget", GetNotApprovedNugetPackages },
            { "Conan", GetNotApprovedConanPackages },
            { "Debian", GetNotApprovedDebianPackages },
            { "Maven", GetNotApprovedMavenPackages },
            { "Poetry", GetNotApprovedPythonPackages },
            { "Choco", GetNotApprovedChocoPackages   },
            { "Cargo", GetNotApprovedCargoPackages   }
        };

                if (packageHandlers.TryGetValue(name, out var handler))
                {
                    handler(unknownPackages, projectResponse, fileOperations, filepath, filename);
                }
            }
        }

        /// <summary>
        /// Gets not approved npm packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown npm packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedNpmPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> npmComponents = new List<JsonComponents>();
                foreach (var npmpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = npmpackage.Name;
                    jsonComponents.Version = npmpackage.Version;
                    npmComponents.Add(jsonComponents);
                }
                myDeserializedClass.Npm = npmComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);

            }
            else
            {
                projectResponse.Npm = new List<JsonComponents>();
                foreach (var npmpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = npmpackage.Name;
                    jsonComponents.Version = npmpackage.Version;
                    projectResponse.Npm.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved NuGet packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown NuGet packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedNugetPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> nugetComponents = new List<JsonComponents>();
                foreach (var nugetpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = nugetpackage.Name;
                    jsonComponents.Version = nugetpackage.Version;
                    nugetComponents.Add(jsonComponents);
                }
                myDeserializedClass.Nuget = nugetComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            else
            {
                projectResponse.Nuget = new List<JsonComponents>();
                foreach (var nugetpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = nugetpackage.Name;
                    jsonComponents.Version = nugetpackage.Version;
                    projectResponse.Nuget.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved Cargo packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Cargo packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedCargoPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> cargoComponents = new List<JsonComponents>();
                foreach (var cargoPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = cargoPackage.Name;
                    jsonComponents.Version = cargoPackage.Version;
                    cargoComponents.Add(jsonComponents);
                }
                myDeserializedClass.Cargo = cargoComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            else
            {
                projectResponse.Cargo = new List<JsonComponents>();
                foreach (var cargoPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = cargoPackage.Name;
                    jsonComponents.Version = cargoPackage.Version;
                    projectResponse.Cargo.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved Conan packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Conan packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedConanPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);

                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> conanComponents = new List<JsonComponents>();
                foreach (var conanpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = conanpackage.Name;
                    jsonComponents.Version = conanpackage.Version;
                    conanComponents.Add(jsonComponents);
                }
                myDeserializedClass.Conan = conanComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);


            }
            else
            {
                projectResponse.Conan = new List<JsonComponents>();
                foreach (var conanpackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = conanpackage.Name;
                    jsonComponents.Version = conanpackage.Version;
                    projectResponse.Conan.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);

        }

        /// <summary>
        /// Gets not approved Python packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Python packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedPythonPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);

                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> pythonComponents = new List<JsonComponents>();
                foreach (var pythonPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = pythonPackage.Name;
                    jsonComponents.Version = pythonPackage.Version;
                    pythonComponents.Add(jsonComponents);
                }
                myDeserializedClass.Python = pythonComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);


            }
            else
            {
                projectResponse.Python = new List<JsonComponents>();
                foreach (var pythonPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = pythonPackage.Name;
                    jsonComponents.Version = pythonPackage.Version;
                    projectResponse.Python.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved Debian packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Debian packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        public static void GetNotApprovedDebianPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);

                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> debianComponents = new List<JsonComponents>();
                foreach (var debianPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = debianPackage.Name;
                    jsonComponents.Version = debianPackage.Version;
                    debianComponents.Add(jsonComponents);
                }
                myDeserializedClass.Debian = debianComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);


            }
            else
            {
                projectResponse.Debian = new List<JsonComponents>();
                foreach (var debianPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = debianPackage.Name;
                    jsonComponents.Version = debianPackage.Version;
                    projectResponse.Debian.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved Maven packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Maven packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        private static void GetNotApprovedMavenPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);

                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> mavenComponents = new List<JsonComponents>();
                foreach (var mavenPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = mavenPackage.Name;
                    jsonComponents.Version = mavenPackage.Version;
                    mavenComponents.Add(jsonComponents);
                }
                myDeserializedClass.Maven = mavenComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);


            }
            else
            {
                projectResponse.Maven = new List<JsonComponents>();
                foreach (var mavenPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = mavenPackage.Name;
                    jsonComponents.Version = mavenPackage.Version;
                    projectResponse.Maven.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Gets not approved Chocolatey packages and writes them to the report file.
        /// </summary>
        /// <param name="unknownPackages">List of unknown Chocolatey packages.</param>
        /// <param name="projectResponse">The project response object.</param>
        /// <param name="fileOperations">The file operations interface.</param>
        /// <param name="filepath">The file path for storing package information.</param>
        /// <param name="filename">The filename for the report.</param>
        public static void GetNotApprovedChocoPackages(List<ComponentsToArtifactory> unknownPackages, ProjectResponse projectResponse, IFileOperations fileOperations, string filepath, string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);

                ProjectResponse myDeserializedClass = JsonConvert.DeserializeObject<ProjectResponse>(json);
                List<JsonComponents> chocoComponents = new List<JsonComponents>();
                foreach (var chocoPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = chocoPackage.Name;
                    jsonComponents.Version = chocoPackage.Version;
                    chocoComponents.Add(jsonComponents);
                }
                myDeserializedClass.Choco = chocoComponents;
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            else
            {
                projectResponse.Choco = new List<JsonComponents>();
                foreach (var chocoPackage in unknownPackages)
                {
                    JsonComponents jsonComponents = new JsonComponents();
                    jsonComponents.Name = chocoPackage.Name;
                    jsonComponents.Version = chocoPackage.Version;
                    projectResponse.Choco.Add(jsonComponents);
                }
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, ReportFileName);
            }
            WarningMessageForNoPackages(filename);
        }

        /// <summary>
        /// Logs a detailed failure message based on uploader KPIs and terminates the process with a non-zero exit code.
        /// </summary>
        /// <param name="uploaderKpiData">
        /// The KPI data containing counts of packages not existing in the repository and packages not actioned due to error.
        /// </param>
        /// <param name="environmentHelper">
        /// The environment helper used to exit the process with the appropriate exit code.
        /// </param>
        /// <remarks>
        /// This method constructs a contextual warning message indicating the cause of failure:
        /// - Packages not existing in repository (remote cache)
        /// - Packages not actioned due to error
        /// If one or both counts are greater than zero, it logs the message and calls <see cref="EnvironmentHelper.CallEnvironmentExit(int)"/> with exit code 2.
        /// </remarks>
        public static void SetExitCode(UploaderKpiData uploaderKpiData,EnvironmentHelper environmentHelper)
        {           
            if (uploaderKpiData.PackagesNotUploadedDueToError > 0 || uploaderKpiData.PackagesNotExistingInRemoteCache > 0)
            {
                // Build a detailed failure message
                var notInRepo = uploaderKpiData.PackagesNotExistingInRemoteCache;
                var notUploadedError = uploaderKpiData.PackagesNotUploadedDueToError;

                string reasonMessage;

                if (notInRepo > 0 && notUploadedError > 0)
                {
                    reasonMessage =
                        $"This step failed due to {notInRepo} packages not found in repository and {notUploadedError} packages not actioned due to error. " +
                        "For more details, review the above tables.";
                }
                else if (notInRepo > 0)
                {
                    reasonMessage =
                        $"This step failed due to {notInRepo} packages not found in repository. " +
                        "For more details, review the above tables.";
                }
                else
                {
                    reasonMessage =
                        $"This step failed due to {notUploadedError} packages not actioned due to error. " +
                        "For more details, review the above tables.";
                }

                Logger.Warn(reasonMessage);
                environmentHelper.CallEnvironmentExit(2);
                Logger.Debug("Setting ExitCode to 2");
            }            
        }
        #endregion
    }
}
