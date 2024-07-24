// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LCT.Common;
using System.Linq;
using LCT.Common.Constants;
using System.IO;
using LCT.Common.Model;
using log4net.Core;

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
            //Reading the CycloneBOM data

            Bom m_ComponentsInBOM = PackageUploadHelper.GetComponentListFromComparisonBOM(appSettings.BomFilePath);

            DisplayAllSettings(m_ComponentsInBOM.Components, appSettings);
            uploaderKpiData.ComponentInComparisonBOM = m_ComponentsInBOM.Components.Count;

            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();

            List<ComponentsToArtifactory> m_ComponentsToBeUploaded = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(m_ComponentsInBOM.Components, appSettings, displayPackagesInfo);
            //Uploading the component to artifactory

            uploaderKpiData.PackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.ClearedThirdParty);
            uploaderKpiData.DevPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Development);
            uploaderKpiData.InternalPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Internal);

            await PackageUploadHelper.UploadingThePackages(m_ComponentsToBeUploaded, appSettings.TimeOut, displayPackagesInfo);

            //Display packages information 
            PackageUploadHelper.DisplayPackageUploadInformation(displayPackagesInfo);


            //Updating the component's new location
            var fileOperations = new FileOperations();
            string bomGenerationPath = Path.GetDirectoryName(appSettings.BomFilePath);
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref m_ComponentsInBOM, m_ComponentsToBeUploaded);

            //update Jfrog Repo Path For Sucessfully Uploaded Items
            m_ComponentsInBOM = await PackageUploadHelper.UpdateJfrogRepoPathForSucessfullyUploadedItems(m_ComponentsInBOM, displayPackagesInfo);

            var formattedString = CycloneDX.Json.Serializer.Serialize(m_ComponentsInBOM);

            // wrtite final out put in the json file
            fileOperations.WriteContentToOutputBomFile(formattedString, bomGenerationPath, 
                FileConstant.BomFileName, appSettings.SW360ProjectName);

            // write kpi info to console table 
            if (Program.UploaderStopWatch != null)
                uploaderKpiData.TimeTakenByComponentCreator =
                TimeSpan.FromMilliseconds(Program.UploaderStopWatch.ElapsedMilliseconds).TotalSeconds;
            PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData);
            
            Logger.Debug($"UploadPackageToArtifactory():End");

            // set the error code
            if (uploaderKpiData.PackagesNotUploadedDueToError > 0 || uploaderKpiData.PackagesNotExistingInRemoteCache > 0)
            {
                Environment.ExitCode = 2;
                Logger.Debug("Setting ExitCode to 2");
            }
        }
        public static void DisplayAllSettings(List<Component> m_ComponentsInBOM, CommonAppSettings appSettings)
        {
            Logger.Info("Current Application Settings:");
            List<string> projectTypeList = new();
            foreach (var item in m_ComponentsInBOM)
            {

                projectTypeList.Add(item.Properties.First(x => x.Name == Dataconstant.Cdx_ProjectType).Value);
            }
            var projectType = projectTypeList.Distinct().ToList();

            for (int i = 0; i < projectType.Count; i++)
            {
                string type = projectType[i];
                Logger.Info($"{projectType[i]}:\n\t");
                switch (type.ToUpperInvariant())
                {
                    case "NPM":
                        PackageSettings(appSettings.Npm);
                        break;
                    case "NUGET":
                        PackageSettings(appSettings.Nuget);
                        break;
                    case "MAVEN":
                        PackageSettings(appSettings.Maven);
                        break;
                    case "DEBIAN":
                        PackageSettings(appSettings.Debian);
                        break;
                    case "PYTHON":
                        PackageSettings(appSettings.Python);
                        break;
                    case "CONAN":
                        PackageSettings(appSettings.Conan);
                        break;

                    default:
                        Logger.Error($"DiplayAllSettings():Invalid ProjectType - {type}");
                        break;
                }

            }

        }
                
        private static void PackageSettings(Config project)
        {
            string includeList = string.Empty;
            string excludeList = string.Empty;
            if (project.Include != null)
            {
                includeList = string.Join(",", project.Include?.ToList());
            }
            if (project.Exclude != null)
            {
                excludeList = string.Join(",", project.Exclude?.ToList());
            }

            Logger.Logger.Log(null, Level.Notice, $"\tDEVDEP_REPO_NAME:\t{project.JfrogDevDestRepoName}\n\t" +
              $"THIRD_PARTY_REPO_NAME:\t{project.JfrogThirdPartyDestRepoName}\n\t" +
              $"INTERNAL_REPO_NAME:\t{project.JfrogInternalDestRepoName}\n\t" +
              $"Config:\n\t" +
              $"Exclude:\t\t{excludeList}\n\t" +
              $"Include: \t\t{includeList}\n", null);
        }
    }
}
