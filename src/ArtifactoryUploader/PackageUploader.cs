// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    /// <summary>
    /// PackageUploader class 
    /// </summary>
    public static class PackageUploader
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly UploaderKpiData uploaderKpiData = new UploaderKpiData();

        public static async Task UploadPackageToArtifactory(CommonAppSettings appSettings)
        {
            Logger.Debug($"UploadPackageToArtifactory():Upload package to artifactory process has started");
            //Reading the CycloneBOM data
            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);
            Bom m_ComponentsInBOM = PackageUploadHelper.GetComponentListFromComparisonBOM(bomFilePath);            
            DisplayAllSettings(m_ComponentsInBOM.Components, appSettings);
            uploaderKpiData.ComponentInComparisonBOM = m_ComponentsInBOM.Components.Count;

            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();

            List<ComponentsToArtifactory> m_ComponentsToBeUploaded = await UploadToArtifactory.GetComponentsToBeUploadedToArtifactory(m_ComponentsInBOM.Components, appSettings, displayPackagesInfo);
            //Uploading the component to artifactory

            uploaderKpiData.PackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.ClearedThirdParty);
            uploaderKpiData.DevPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Development);
            uploaderKpiData.InternalPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Internal);

            await PackageUploadHelper.UploadingThePackages(m_ComponentsToBeUploaded, appSettings.TimeOut, displayPackagesInfo);

            //Display packages information 
            PackageUploadInformation.DisplayPackageUploadInformation(displayPackagesInfo);


            //Updating the component's new location
            var fileOperations = new FileOperations();
            string bomGenerationPath = Path.GetDirectoryName(bomFilePath);
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref m_ComponentsInBOM, m_ComponentsToBeUploaded);

            //update Jfrog Repository Path For Successfully Uploaded Items
            m_ComponentsInBOM = await JfrogRepoUpdater.UpdateJfrogRepoPathForSucessfullyUploadedItems(m_ComponentsInBOM, displayPackagesInfo);

            var formattedString = CycloneDX.Json.Serializer.Serialize(m_ComponentsInBOM);

            // write final out put in the JSON file
            fileOperations.WriteContentToOutputBomFile(formattedString, bomGenerationPath,
                FileConstant.BomFileName, appSettings.SW360.ProjectName);

            // write KPI info to console table 
            if (Program.UploaderStopWatch != null)
                uploaderKpiData.TimeTakenByArtifactoryUploader =
                TimeSpan.FromMilliseconds(Program.UploaderStopWatch.ElapsedMilliseconds).TotalSeconds;
            PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData);

            Logger.Debug($"UploadPackageToArtifactory():Upload package to artifactory process has completed");

            // set the error code
            if (uploaderKpiData.PackagesNotUploadedDueToError > 0 || uploaderKpiData.PackagesNotExistingInRemoteCache > 0)
            {
                EnvironmentHelper environmentHelper = new EnvironmentHelper();
                environmentHelper.CallEnvironmentExit(2);
                Logger.Debug("Setting ExitCode to 2");
            }
        }
        public static void DisplayAllSettings(List<Component> componentsInBOM, CommonAppSettings appSettings)
        {
            Logger.Info("Current Application Settings:");

            // Get distinct project types from the BOM components
            var projectTypes = componentsInBOM
                .Select(item => item.Properties.First(x => x.Name == Dataconstant.Cdx_ProjectType).Value)
                .Distinct()
                .ToList();

            Logger.Debug($"Project Types found in BOM: {string.Join(", ", projectTypes)}");
            foreach (var projectType in projectTypes)
            {
                Logger.Info($"{projectType}:\n\t");

                // Use a dictionary to map project types to their configurations for cleaner logic
                var projectConfigMap = new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NPM", appSettings.Npm },
                    { "NUGET", appSettings.Nuget },
                    { "MAVEN", appSettings.Maven },
                    { "DEBIAN", appSettings.Debian },
                    { "POETRY", appSettings.Poetry },
                    { "CONAN", appSettings.Conan }
                };

                if (projectConfigMap.TryGetValue(projectType, out var config))
                {
                    DisplayPackageSettings(config);
                }
                else
                {
                    Logger.ErrorFormat("DisplayAllSettings(): Invalid ProjectType - {0}", projectType);
                }
            }
        }

        private static void DisplayPackageSettings(Config project)
        {
            if (project == null)
            {
                Logger.Debug("DisplayPackageSettings(): Configuration for the project is null. Please ensure the project settings are properly configured in the appsettings.");
                Logger.Warn("DisplayPackageSettings(): Config is null.");
                return;
            }

            // Build Include, Exclude, and ThirdPartyRepoName strings safely
            string includeList = !string.IsNullOrEmpty(project.Include?.FirstOrDefault())
                ? string.Join(", ", project.Include)
                : Dataconstant.NotConfigured;
            Logger.Debug($"DisplayPackageSettings(): Include files list from appsettings:{includeList}");
            
            string excludeList = !string.IsNullOrEmpty(project.Exclude?.FirstOrDefault())
                ? string.Join(", ", project.Exclude)
                : Dataconstant.NotConfigured;
            Logger.Debug($"DisplayPackageSettings(): Exclude files list from appsettings:{excludeList}");

            string devDepRepoName = !string.IsNullOrEmpty(project.DevDepRepo)
                ? project.DevDepRepo
                : Dataconstant.NotConfigured;
            Logger.Debug($"DisplayPackageSettings(): Dev Dependency Repository Name from appsettings:{devDepRepoName}");

            string releaseRepoName = !string.IsNullOrEmpty(project.ReleaseRepo)
                ? project.ReleaseRepo
                : Dataconstant.NotConfigured;
            Logger.Debug($"DisplayPackageSettings(): Release Repository Name from appsettings:{releaseRepoName}");

            string thirdPartyRepoName = project.Artifactory?.ThirdPartyRepos?
                .FirstOrDefault(repo => repo.Upload)?.Name ?? Dataconstant.NotConfigured;
            Logger.Debug($"Third Party Repository Name from appsettings: {thirdPartyRepoName}");

            // Log the settings for the project
            Logger.Logger.Log(null, Level.Notice,
                $"\tDEVDEP_REPO_NAME:\t{project.DevDepRepo}\n" +
                $"\tTHIRD_PARTY_REPO_NAME:\t{thirdPartyRepoName}\n" +
                $"\tRELEASE_REPO_NAME:\t{project.ReleaseRepo}\n" +
                $"\tConfig:\n" +
                $"\t\tExclude:\t\t{excludeList}\n" +
                $"\t\tInclude:\t\t{includeList}\n", null);
        }
    }
}
