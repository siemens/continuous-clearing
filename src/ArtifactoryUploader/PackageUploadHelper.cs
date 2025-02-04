// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Services.Interface;
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
using Directory = System.IO.Directory;

namespace LCT.ArtifactoryUploader
{
    /// <summary>
    /// PackageUploaderHelper class  - Reads,collect packages to upload
    /// </summary>
    public static class PackageUploadHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IJFrogService jFrogService { get; set; }
        private static List<AqlResult> aqlResultList = new();

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
                    componentsToBoms = CycloneDX.Json.Serializer.Deserialize(json);
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
        
        private static Task<ComponentsToArtifactory> GetPackageinfo(ComponentsToArtifactory item, string operationType, HttpResponseMessage responseMessage, string dryRunSuffix)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,
                SrcRepoName = item.SrcRepoName,
                DestRepoName = item.DestRepoName,
                OperationType = operationType,
                ResponseMessage = responseMessage,
                DryRunSuffix = dryRunSuffix,
                ComponentType = item.ComponentType,
                Purl = item.Purl,
                Token = item.Token,
                CopyPackageApiUrl = item.CopyPackageApiUrl,
                PackageName = item.PackageName,
                PackageType = item.PackageType,

            };
            return Task.FromResult(components);

        }
        private static Task<ComponentsToArtifactory> GetSucessFulPackageinfo(ComponentsToArtifactory item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,
                SrcRepoName = item.SrcRepoName,
                DestRepoName = item.DestRepoName,
                SrcRepoPathWithFullName = item.SrcRepoPathWithFullName,
                Path = item.Path,
                PackageType = item.PackageType,
                Purl = item.Purl,

            };
            return Task.FromResult(components);

        }
       

        public static async Task JfrogNotFoundPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo)
        {

            if (item.ComponentType == "NPM")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "POETRY")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesConan.Add(components);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.JfrogNotFoundPackagesDebian.Add(components);
            }

        }

        public static async Task JfrogFoundPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo, string operationType, HttpResponseMessage responseMessage, string dryRunSuffix)
        {

            if (item.ComponentType == "NPM")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "POETRY")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesConan.Add(components);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                displayPackagesInfo.JfrogFoundPackagesDebian.Add(components);
            }

        }
        private static async Task SucessfullPackagesAsync(ComponentsToArtifactory item, DisplayPackagesInfo displayPackagesInfo)
        {
            if (item.ComponentType == "NPM")
            {

                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "POETRY")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesConan.Add(components);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                displayPackagesInfo.SuccessfullPackagesDebian.Add(components);
            }

        }


        public static async Task UploadingThePackages(List<ComponentsToArtifactory> componentsToUpload, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("Starting UploadingThePackages() method");
            foreach (var item in componentsToUpload)
            {
                await PackageUploadToArtifactory(PackageUploader.uploaderKpiData, item, timeout, displayPackagesInfo);
            }

            if (SetWarningCode)
            {
                PipelineArtifactUploader.UploadArtifacts();
                Environment.ExitCode = 2;
                Logger.Debug("Setting ExitCode to 2");
            }

            Logger.Debug("Ending UploadingThePackages() method");
            Program.UploaderStopWatch?.Stop();
        }

        private static async Task PackageUploadToArtifactory(UploaderKpiData uploaderKpiData,
                                                             ComponentsToArtifactory item,
                                                             int timeout,
                                                             DisplayPackagesInfo displayPackagesInfo)
        {
            var packageType = item.PackageType;
            if (item.SrcRepoName != null
                && !(item.SrcRepoName.Equals(item.DestRepoName, StringComparison.OrdinalIgnoreCase))
                && !item.SrcRepoName.Contains("siparty-release"))
            {
                if (!(item.SrcRepoName.Contains("Not Found in JFrog")))
                {
                    await SourceRepoFoundToUploadArtifactory(packageType, uploaderKpiData, item, timeout, displayPackagesInfo);
                }
                else
                {
                    uploaderKpiData.PackagesNotExistingInRemoteCache++;
                    item.DestRepoName = null;
                    await JfrogNotFoundPackagesAsync(item, displayPackagesInfo);
                }
            }
            else
            {
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, true);
                await SucessfullPackagesAsync(item, displayPackagesInfo);
                item.DestRepoName = null;
            }
        }

        private static async Task SourceRepoFoundToUploadArtifactory(PackageType packageType, UploaderKpiData uploaderKpiData, ComponentsToArtifactory item, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {
            const string dryRunSuffix = null;
            string operationType = item.PackageType == PackageType.ClearedThirdParty || item.PackageType == PackageType.Development ? "copy" : "move";
            ArtfactoryUploader.jFrogService = jFrogService;
            ArtfactoryUploader.JFrogApiCommInstance = GetJfrogApiCommInstance(item, timeout);
            HttpResponseMessage responseMessage = await ArtfactoryUploader.UploadPackageToRepo(item, timeout, displayPackagesInfo);

            if (responseMessage.StatusCode == HttpStatusCode.OK && !item.DryRun)
            {
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, true);
            }
            else if (responseMessage.ReasonPhrase == ApiConstant.PackageNotFound)
            {
                await JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, false);
                item.DestRepoName = null;
                SetWarningCode = true;
            }
            else if (responseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
            {
                await JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, false);
                item.DestRepoName = null;
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                Logger.Debug($"JFrog Response - {responseContent}");
            }
            else
            {
                // do nothing
            }
        }

        public static IJFrogApiCommunication GetJfrogApiCommInstance(ComponentsToArtifactory component, int timeout)
        {

            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials()
            {
                Token = component.Token,
            };

            // Initialize JFrog API communication based on Component Type
            IJFrogApiCommunication jfrogApicommunication = component.ComponentType?.ToUpperInvariant() switch
            {
                "MAVEN" => new MavenJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout),
                "POETRY" => new PythonJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout),
                _ => new NpmJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout)
            };
            return jfrogApicommunication;
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

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.DevPackagesToBeUploaded)),
                    uploaderKpiData.DevPackagesToBeUploaded},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.DevPackagesUploaded)),
                    uploaderKpiData.DevPackagesUploaded},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.DevPackagesNotUploadedToJfrog)),
                    uploaderKpiData.DevPackagesNotUploadedToJfrog},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.InternalPackagesToBeUploaded)),
                    uploaderKpiData.InternalPackagesToBeUploaded},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.InternalPackagesUploaded)),
                    uploaderKpiData.InternalPackagesUploaded},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.InternalPackagesNotUploadedToJfrog)),
                    uploaderKpiData.InternalPackagesNotUploadedToJfrog},

                { CommonHelper.Convert(uploaderKpiData,nameof(uploaderKpiData.PackagesNotExistingInRemoteCache)),
                    uploaderKpiData.PackagesNotExistingInRemoteCache},

                {CommonHelper.Convert(uploaderKpiData, nameof(uploaderKpiData.PackagesNotUploadedDueToError)),
                    uploaderKpiData.PackagesNotUploadedDueToError}
            };

            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "Artifactory Uploader",uploaderKpiData.TimeTakenByArtifactoryUploader }
            };

            CommonHelper.WriteToConsoleTable(printList, printTimingList);
        }

        private static void IncrementCountersBasedOnPackageType(UploaderKpiData uploaderKpiData, PackageType packageType, bool isSuccess)
        {
            // Define a dictionary to map package types to counters
            Dictionary<PackageType, Action> successActions = new Dictionary<PackageType, Action>
            {
                { PackageType.Internal, () => uploaderKpiData.InternalPackagesUploaded++ },
                { PackageType.Development, () => uploaderKpiData.DevPackagesUploaded++ },
                { PackageType.ClearedThirdParty, () => uploaderKpiData.PackagesUploadedToJfrog++ },
            };

            Dictionary<PackageType, Action> failureActions = new Dictionary<PackageType, Action>
            {
                { PackageType.Internal, () => { uploaderKpiData.InternalPackagesNotUploadedToJfrog++; uploaderKpiData.PackagesNotUploadedDueToError++; } },
                { PackageType.Development, () => { uploaderKpiData.DevPackagesNotUploadedToJfrog++; uploaderKpiData.PackagesNotUploadedDueToError++; } },
                { PackageType.ClearedThirdParty, () => {uploaderKpiData.PackagesNotUploadedToJfrog++; uploaderKpiData.PackagesNotUploadedDueToError++; } },
            };

            if (isSuccess)
            {
                if (successActions.TryGetValue(packageType, out var action))
                {
                    action.Invoke();
                }
            }
            else
            {
                if (failureActions.TryGetValue(packageType, out var action))
                {
                    action.Invoke();
                }
            }
        }

      
        public static void UpdateBomArtifactoryRepoUrl(ref Bom bom, List<ComponentsToArtifactory> componentsUploaded)
        {
            foreach (var component in componentsUploaded)
            {
                var bomComponent = bom.Components.Find(x => x.Purl.Equals(component.Purl, StringComparison.OrdinalIgnoreCase));
                if (component.DestRepoName != null && !component.DryRun)
                {
                    bomComponent.Properties.First(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName).Value = component.DestRepoName;
                }
            }
        }      
               
    }

}
