﻿// --------------------------------------------------------------------------------------------------------------------
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
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace LCT.ArtifactoryUploader
{
    public class PackageUploadInformation
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static DisplayPackagesInfo GetComponentsToBePackages()
        {
            DisplayPackagesInfo displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.UnknownPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.UnknownPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.SuccessfullPackagesDebian = new List<ComponentsToArtifactory>();


            return displayPackagesInfo;

        }
        public static List<ComponentsToArtifactory> GetUploadePackageDetails(DisplayPackagesInfo displayPackagesInfo)
        {
            List<ComponentsToArtifactory> uploadedPackages = new List<ComponentsToArtifactory>();

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesConan)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesMaven)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesNpm)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesNuget)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesPython)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            foreach (var item in displayPackagesInfo.JfrogFoundPackagesDebian)
            {
                if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                {
                    uploadedPackages.Add(item);
                }
            }

            return uploadedPackages;
        }
        public static void DisplayPackageUploadInformation(DisplayPackagesInfo displayPackagesInfo)
        {
            string localPathforartifactory = ArtfactoryUploader.GettPathForArtifactoryUpload();

            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNpm, displayPackagesInfo.JfrogNotFoundPackagesNpm, displayPackagesInfo.SuccessfullPackagesNpm, displayPackagesInfo.JfrogFoundPackagesNpm, "Npm", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesNuget, displayPackagesInfo.JfrogNotFoundPackagesNuget, displayPackagesInfo.SuccessfullPackagesNuget, displayPackagesInfo.JfrogFoundPackagesNuget, "Nuget", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesMaven, displayPackagesInfo.JfrogNotFoundPackagesMaven, displayPackagesInfo.SuccessfullPackagesMaven, displayPackagesInfo.JfrogFoundPackagesMaven, "Maven", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesConan, displayPackagesInfo.JfrogNotFoundPackagesConan, displayPackagesInfo.SuccessfullPackagesConan, displayPackagesInfo.JfrogFoundPackagesConan, "Conan", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesPython, displayPackagesInfo.JfrogNotFoundPackagesPython, displayPackagesInfo.SuccessfullPackagesPython, displayPackagesInfo.JfrogFoundPackagesPython, "Poetry", localPathforartifactory);
            DisplaySortedForeachComponents(displayPackagesInfo.UnknownPackagesDebian, displayPackagesInfo.JfrogNotFoundPackagesDebian, displayPackagesInfo.SuccessfullPackagesDebian, displayPackagesInfo.JfrogFoundPackagesDebian, "Debian", localPathforartifactory);

        }
        private static void DisplaySortedForeachComponents(List<ComponentsToArtifactory> unknownPackages, List<ComponentsToArtifactory> JfrogNotFoundPackages, List<ComponentsToArtifactory> SucessfullPackages, List<ComponentsToArtifactory> JfrogFoundPackages, string name, string filename)
        {
            if (unknownPackages.Any() || JfrogNotFoundPackages.Any() || SucessfullPackages.Any() || JfrogFoundPackages.Any())
            {
                Logger.Info("\n" + name + ":\n");
                DisplayErrorForUnknownPackages(unknownPackages, name, filename);
                DisplayErrorForJfrogFoundPackages(JfrogFoundPackages);
                DisplayErrorForJfrogPackages(JfrogNotFoundPackages);
                DisplayErrorForSucessfullPackages(SucessfullPackages);
            }

        }
        public static void DisplayErrorForJfrogFoundPackages(List<ComponentsToArtifactory> JfrogFoundPackages)
        {

            if (JfrogFoundPackages.Any())
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

        public static void DisplayErrorForJfrogPackages(List<ComponentsToArtifactory> JfrogNotFoundPackages)
        {

            if (JfrogNotFoundPackages.Any())
            {

                foreach (var jfrogNotFoundPackage in JfrogNotFoundPackages)
                {
                    Logger.Warn($"Package {jfrogNotFoundPackage.Name}-{jfrogNotFoundPackage.Version} is not found in jfrog");

                }
                Logger.Info("\n");

            }
        }
        private static void DisplayErrorForSucessfullPackages(List<ComponentsToArtifactory> SucessfullPackages)
        {

            if (SucessfullPackages.Any())
            {

                foreach (var sucessfullPackage in SucessfullPackages)
                {
                    Logger.Info($"Package {sucessfullPackage.Name}-{sucessfullPackage.Version} is already uploaded");
                }
                Logger.Info("\n");

            }
        }
        private static void DisplayErrorForUnknownPackages(List<ComponentsToArtifactory> unknownPackages, string name, string filepath)
        {
            ProjectResponse projectResponse = new ProjectResponse();
            IFileOperations fileOperations = new FileOperations();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            if (unknownPackages.Any())
            {
                var packageHandlers = new Dictionary<string, Action<List<ComponentsToArtifactory>, ProjectResponse, IFileOperations, string, string>>
        {
            { "Npm", GetNotApprovedNpmPackages },
            { "Nuget", GetNotApprovedNugetPackages },
            { "Conan", GetNotApprovedConanPackages },
            { "Debian", GetNotApprovedDebianPackages },
            { "Maven", GetNotApprovedMavenPackages },
            { "Poetry", GetNotApprovedPythonPackages }
        };

                if (packageHandlers.TryGetValue(name, out var handler))
                {
                    handler(unknownPackages, projectResponse, fileOperations, filepath, filename);
                }
            }
        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");

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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");

        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");


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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");

        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");


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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");


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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }
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
                fileOperations.WriteContentToReportNotApprovedFile(myDeserializedClass, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");


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
                fileOperations.WriteContentToReportNotApprovedFile(projectResponse, filepath, FileConstant.artifactoryReportNotApproved, "Artifactory");
            }
            Logger.Warn($"Artifactory upload will not be done due to Report not in Approved state and package details can be found at {filename}\n");
        }

    }
}
