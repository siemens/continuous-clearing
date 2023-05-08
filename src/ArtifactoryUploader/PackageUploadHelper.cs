﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

//---------------------------------------------------------------------------------------------------------------------
using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    /// <summary>
    /// PackageUploaderHelper class  - Reads,collect packages to upload
    /// </summary>
    public static class PackageUploadHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static bool SetWarningCode;
        public static Bom GetComponentListFromComparisonBOM(string comparisionBomFilePath)
        {
            Logger.Debug("Starting GetComponentListFromComparisonBOM() method");
            Bom componentsToBoms = null;
            try
            {
                if (File.Exists(comparisionBomFilePath))
                {
                    string json = File.ReadAllText(comparisionBomFilePath);
                    componentsToBoms = JsonConvert.DeserializeObject<Bom>(json);
                }
                else
                {
                    throw new FileNotFoundException($"File :- {comparisionBomFilePath} is not found.Enter a valid file path");
                }
            }
            catch (JsonReaderException ex)
            {

                Logger.Error($"Exception occured in reading the comparison BOM: {ex}");
                throw new JsonReaderException();

            }
            return componentsToBoms;
        }

        public static List<ComponentsToArtifactory> GetComponentsToBeUploadedToArtifactory(List<Component> comparisonBomData, CommonAppSettings appSettings)
        {
            Logger.Debug("Starting GetComponentsToBeUploadedToArtifactory() method");
            List<ComponentsToArtifactory> componentsToBeUploaded = new List<ComponentsToArtifactory>();

            foreach (var item in comparisonBomData)
            {

                if (item.Properties.Any(p => p.Name == Dataconstant.Cdx_ClearingState && p.Value.ToUpperInvariant() == "APPROVED"))
                {

                    ComponentsToArtifactory components = new ComponentsToArtifactory()
                    {
                        Name = !string.IsNullOrEmpty(item.Group) ? $"{item.Group}/{item.Name}" : item.Name,
                        PackageName = item.Name,
                        Version = item.Version,
                        ComponentType = GetComponentType(item),
                        SrcRepoName = item.Properties[1].Value,
                        DestRepoName = GetDestinationRepo(item, appSettings),
                        ApiKey = appSettings.ArtifactoryUploadApiKey,
                        Email = appSettings.ArtifactoryUploadUser,
                        JfrogApi = appSettings.JFrogApi
                    };
                    components.PackageInfoApiUrl = GetPackageInfoURL(components);
                    components.CopyPackageApiUrl = GetCopyURL(components);
                    componentsToBeUploaded.Add(components);
                }
                else
                {
                    PackageUploader.uploaderKpiData.ComponentNotApproved++;
                    PackageUploader.uploaderKpiData.PackagesNotUploadedToJfrog++;
                    Logger.Warn($"Package {item.Name}-{item.Version} is not in report approved state,hence artifactory upload will not be done!");
                }
            }
            Logger.Debug("Ending GetComponentsToBeUploadedToArtifactory() method");
            return componentsToBeUploaded;
        }

        private static string GetCopyURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == "NPM")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Name}/-/{component.PackageName}-{component.Version}" +
              $"{ApiConstant.NpmExtension}?to=/{component.DestRepoName}/{component.Name}/-/{component.PackageName}-{component.Version}{ApiConstant.NpmExtension}";
            }
            else if (component.ComponentType == "NUGET")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}" +
               $"{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == "MAVEN")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Name}/{component.Version}" +
               $"?to=/{component.DestRepoName}/{component.Name}/{component.Version}";
            }
            else
            {
                // Do nothing
            }
            return url;
        }

        private static string GetPackageInfoURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == "NPM")
            {
                url = $"{component.JfrogApi}{ApiConstant.PackageInfoApi}{component.SrcRepoName}/{component.Name}/-/{component.PackageName}-{component.Version}{ApiConstant.NpmExtension}";
            }
            else if (component.ComponentType == "NUGET")
            {
                url = $"{component.JfrogApi}{ApiConstant.PackageInfoApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == "MAVEN")
            {
                url = $"{component.JfrogApi}{ApiConstant.PackageInfoApi}{component.SrcRepoName}/{component.Name}/{component.Version}";
            }
            else
            {
                // Do nothing
            }
            return url;
        }

        private static string GetDestinationRepo(Component item, CommonAppSettings appSettings)
        {
            if (item.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                return appSettings.JfrogNpmDestRepoName;
            }
            else if (item.Purl.Contains("nuget", StringComparison.OrdinalIgnoreCase))
            {
                return appSettings.JfrogNugetDestRepoName;
            }
            else if (item.Purl.Contains("maven", StringComparison.OrdinalIgnoreCase))
            {
                return appSettings.JfrogMavenDestRepoName;
            }
            else
            {
                // Do nothing
            }
            return string.Empty;
        }

        private static string GetComponentType(Component item)
        {

            if (item.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                return "NPM";
            }
            else if (item.Purl.Contains("nuget", StringComparison.OrdinalIgnoreCase))
            {
                return "NUGET";
            }
            else if (item.Purl.Contains("maven", StringComparison.OrdinalIgnoreCase))
            {
                return "MAVEN";
            }
            else
            {
                // Do nothing
            }
            return string.Empty;
        }

        public static async Task UploadingThePackages(List<ComponentsToArtifactory> componentsToUpload)
        {
            Logger.Debug("Starting UploadingThePackages() method");
            foreach (var item in componentsToUpload)
            {
                await PackageUploadToArtifactory(PackageUploader.uploaderKpiData, item);
            }

            if (SetWarningCode)
            {
                Environment.ExitCode = 2;
                Logger.Debug("Setting ExitCode to 2");
            }

            Logger.Debug("Ending UploadingThePackages() method");
            Program.UploaderStopWatch?.Stop();
        }

        private static async Task PackageUploadToArtifactory(UploaderKpiData uploaderKpiData, ComponentsToArtifactory item)
        {
            if (!(item.SrcRepoName.Contains("siparty")))
            {
                if (!(item.SrcRepoName.Contains("Not Found in JFrog")))
                {

                    ArtfactoryUploader artifactoryUpload = new ArtfactoryUploader();
                    HttpResponseMessage responseMessage = await artifactoryUpload.UploadPackageToRepo(item);
                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        uploaderKpiData.PackagesUploadedToJfrog++;
                    }
                    else if (responseMessage.ReasonPhrase == ApiConstant.PackageNotFound)
                    {
                        Logger.Error($"Package {item.Name}-{item.Version} not found in remote cache, Upload Failed!!");
                        uploaderKpiData.PackagesNotUploadedToJfrog++;
                        SetWarningCode = true;
                    }
                    else if (responseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
                    {
                        Logger.Error($"Package {item.Name}-{item.Version} Upload Failed!!");
                        uploaderKpiData.PackagesNotUploadedToJfrog++;
                        uploaderKpiData.PackagesNotUploadedDueToError++;
                    }
                    else
                    {
                        // do nothing
                    }
                }
                else
                {

                    uploaderKpiData.PackagesNotExistingInRemoteCache++;
                    Logger.Warn($"Package {item.Name}-{item.Version} is not found in jfrog");
                }


            }
            else
            {
                uploaderKpiData.PackagesUploadedToJfrog++;
                Logger.Warn($"Package {item.Name}-{item.Version} is already uploaded to {item.DestRepoName}");
            }
        }

        public static void WriteCreatorKpiDataToConsole(UploaderKpiData uploaderKpiData)
        {
            Dictionary<string, int> printList = new Dictionary<string, int>()
            {
                {CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.ComponentInComparisonBOM)),
                    uploaderKpiData.ComponentInComparisonBOM },
                {CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.ComponentNotApproved)),
                    uploaderKpiData.ComponentNotApproved },
                {CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.PackagesToBeUploaded)),
                    uploaderKpiData.PackagesToBeUploaded },

                {CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.PackagesUploadedToJfrog)),
                    uploaderKpiData.PackagesUploadedToJfrog },

                { CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.PackagesNotUploadedToJfrog)),
                    uploaderKpiData.PackagesNotUploadedToJfrog},

                { CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.PackagesNotExistingInRemoteCache)),
                    uploaderKpiData.PackagesNotExistingInRemoteCache},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.PackagesNotUploadedDueToError)),
                    uploaderKpiData.PackagesNotUploadedDueToError}
            };

            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "Artifactory Uploader",uploaderKpiData.TimeTakenByComponentCreator }
            };

            CommonHelper.WriteToConsoleTable(printList, printTimingList);
        }

    }

}
