// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Services;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        public async static Task<List<ComponentsToArtifactory>> GetComponentsToBeUploadedToArtifactory(List<Component> comparisonBomData, CommonAppSettings appSettings, UnknownPackagesAll unknownPackagesAll)
        {
            Logger.Debug("Starting GetComponentsToBeUploadedToArtifactory() method");
            List<ComponentsToArtifactory> componentsToBeUploaded = new List<ComponentsToArtifactory>();

            foreach (var item in comparisonBomData)
            {
                var packageType = GetPackageType(item);
                if (packageType != PackageType.Unknown)
                {
                    AqlResult aqlResult = await GetSrcRepoDetailsForPyPiOrConanPackages(item);
                    ComponentsToArtifactory components = new ComponentsToArtifactory()
                    {
                        Name = !string.IsNullOrEmpty(item.Group) ? $"{item.Group}/{item.Name}" : item.Name,
                        PackageName = item.Name,
                        Version = item.Version,
                        Purl = item.Purl,
                        ComponentType = GetComponentType(item),
                        PackageType = packageType,
                        DryRun = !appSettings.Release,
                        SrcRepoName = item.Properties.Find(s => s.Name == Dataconstant.Cdx_ArtifactoryRepoUrl)?.Value,
                        DestRepoName = GetDestinationRepo(item, appSettings),
                        ApiKey = appSettings.ArtifactoryUploadApiKey,
                        Email = appSettings.ArtifactoryUploadUser,
                        JfrogApi = appSettings.JFrogApi,
                    };

                    if (aqlResult != null)
                    {
                        components.SrcRepoPathWithFullName = aqlResult.Repo + "/" + aqlResult.Path + "/" + aqlResult.Name;
                        components.PypiCompName = aqlResult.Name;
                    }
                    else
                    {
                        components.SrcRepoPathWithFullName = string.Empty;
                        components.PypiCompName = string.Empty;
                    }

                    components.Path = GetPackagePath(components, aqlResult);
                    components.CopyPackageApiUrl = GetCopyURL(components);
                    components.MovePackageApiUrl = GetMoveURL(components);
                    components.JfrogPackageName = GetJfrogPackageName(components);
                    componentsToBeUploaded.Add(components);
                }
                else
                {
                    PackageUploader.uploaderKpiData.ComponentNotApproved++;
                    PackageUploader.uploaderKpiData.PackagesNotUploadedToJfrog++;
                    await AddUnknownPackagesAsync(item,unknownPackagesAll);
                }
            }
            
            Logger.Debug("Ending GetComponentsToBeUploadedToArtifactory() method");
            return componentsToBeUploaded;
        }

        public static UnknownPackagesAll GetComponentsToBePackages()
        {
            UnknownPackagesAll unknownPackagesAll = new UnknownPackagesAll();
            unknownPackagesAll.UnknownPackagesNpm = new List<ComponentsToArtifactory>();
            unknownPackagesAll.UnknownPackagesNuget = new List<ComponentsToArtifactory>();
            unknownPackagesAll.UnknownPackagesMaven = new List<ComponentsToArtifactory>();
            unknownPackagesAll.UnknownPackagesConan = new List<ComponentsToArtifactory>();
            unknownPackagesAll.UnknownPackagesPython = new List<ComponentsToArtifactory>();
            unknownPackagesAll.UnknownPackagesDebian = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesPython = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesConan = new List<ComponentsToArtifactory>();
            unknownPackagesAll.JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesNpm = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesNuget = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesPython = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesMaven = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesConan = new List<ComponentsToArtifactory>();
            unknownPackagesAll.SuccessfullPackagesDebian = new List<ComponentsToArtifactory>();


            return unknownPackagesAll;

        }

        private static void DisplaySortedForeachComponents(List<ComponentsToArtifactory> unknownPackages, List<ComponentsToArtifactory> JfrogNotFoundPackages, List<ComponentsToArtifactory> SucessfullPackages, List<ComponentsToArtifactory> JfrogFoundPackages, string name)
        {
            if (unknownPackages.Any()||JfrogNotFoundPackages.Any()||SucessfullPackages.Any()|| JfrogFoundPackages.Any())
            {
                Logger.Info("\n" + name + "\n");
                DisplayErrorForUnknownPackages(unknownPackages);
                DisplayErrorForJfrogFoundPackages(JfrogFoundPackages);
                DisplayErrorForJfrogPackages(JfrogNotFoundPackages);
                DisplayErrorForSucessfullPackages(SucessfullPackages);
            }

        }

        private static void DisplayErrorForJfrogFoundPackages(List<ComponentsToArtifactory> JfrogFoundPackages)
        {

            if (JfrogFoundPackages.Any())
            {
                
                foreach (var jfrogFoundPackage in JfrogFoundPackages)
                {
                     Logger.Info($"Successful{jfrogFoundPackage.DryRunSuffix} {jfrogFoundPackage.OperationType} package {jfrogFoundPackage.PackageName}-{jfrogFoundPackage.Version}" +
                    $" from {jfrogFoundPackage.SrcRepoName} to {jfrogFoundPackage.DestRepoName}");

                    if (jfrogFoundPackage.ResponseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
                    {
                        Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} {jfrogFoundPackage.OperationType} Failed!! {jfrogFoundPackage.SrcRepoName} ---> {jfrogFoundPackage.DestRepoName}");
                    }
                    else if(jfrogFoundPackage.ResponseMessage.ReasonPhrase==ApiConstant.PackageNotFound)
                    {
                        Logger.Error($"Package {jfrogFoundPackage.Name}-{jfrogFoundPackage.Version} not found in {jfrogFoundPackage.SrcRepoName}, Upload Failed!!");
                    }

                }
                Logger.Info("\n");

            }
        }

        private static void DisplayErrorForJfrogPackages(List<ComponentsToArtifactory> JfrogNotFoundPackages)
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
        private static void DisplayErrorForUnknownPackages(List<ComponentsToArtifactory> unknownPackages)
        {

            if (unknownPackages.Any())
            {
                
                foreach (var unknownPackage in unknownPackages)
                {
                    Logger.Warn($"Package {unknownPackage.Name}-{unknownPackage.Version} is not in report approved state,hence artifactory upload will not be done!");
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
        public static void DisplayPackageUploadInformation(UnknownPackagesAll unknownPackagesAlls)
        {
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesNpm,unknownPackagesAlls.JfrogNotFoundPackagesNpm,unknownPackagesAlls.SuccessfullPackagesNpm,unknownPackagesAlls.JfrogFoundPackagesNpm, "NPM:");
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesNuget, unknownPackagesAlls.JfrogNotFoundPackagesNuget, unknownPackagesAlls.SuccessfullPackagesNuget, unknownPackagesAlls.JfrogFoundPackagesNuget, "Nuget:");
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesMaven, unknownPackagesAlls.JfrogNotFoundPackagesMaven, unknownPackagesAlls.SuccessfullPackagesMaven, unknownPackagesAlls.JfrogFoundPackagesMaven, "Maven:");
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesConan, unknownPackagesAlls.JfrogNotFoundPackagesConan, unknownPackagesAlls.SuccessfullPackagesConan, unknownPackagesAlls.JfrogFoundPackagesConan, "Conan:");
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesPython, unknownPackagesAlls.JfrogNotFoundPackagesPython, unknownPackagesAlls.SuccessfullPackagesPython, unknownPackagesAlls.JfrogFoundPackagesPython, "Python:");
            DisplaySortedForeachComponents(unknownPackagesAlls.UnknownPackagesDebian, unknownPackagesAlls.JfrogNotFoundPackagesDebian, unknownPackagesAlls.SuccessfullPackagesDebian, unknownPackagesAlls.JfrogFoundPackagesDebian, "Debian:");

        }

        private static Task<ComponentsToArtifactory> GetUnknownPackageinfo(Component item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version
            };
            return Task.FromResult(components);

        }

        private static Task<ComponentsToArtifactory> GetPackageinfo(ComponentsToArtifactory item,string operationType, HttpResponseMessage responseMessage,string dryRunSuffix)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,
                SrcRepoName = item.SrcRepoName,
                DestRepoName = item.DestRepoName,
                OperationType = operationType,
                ResponseMessage = responseMessage,
                DryRunSuffix = dryRunSuffix
                
            };
            return Task.FromResult(components);

        }
        private static Task<ComponentsToArtifactory> GetSucessFulPackageinfo(ComponentsToArtifactory item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version,               
               
            };
            return Task.FromResult(components);

        }

        private static async Task AddUnknownPackagesAsync(Component item, UnknownPackagesAll unknownPackagesAll)
        {
            string GetPropertyValue(string propertyName) =>
                  item.Properties
                      .Find(p => p.Name == propertyName)?
                      .Value?
                      .ToUpperInvariant();
                        
            if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "NPM")
            {
                
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesNpm.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "NUGET")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesNuget.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "MAVEN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesPython.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "PYTHON")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesMaven.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "CONAN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesConan.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                unknownPackagesAll.UnknownPackagesDebian.Add(components);
            }

        }

        private static async Task JfrogNotFoundPackagesAsync(ComponentsToArtifactory item, UnknownPackagesAll unknownPackagesAll)
        {
            
            if (item.ComponentType == "NPM")
            {

                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "PYTHON")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesConan.Add(components);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.JfrogNotFoundPackagesDebian.Add(components);
            }

        }

        public static async Task JfrogFoundPackagesAsync(ComponentsToArtifactory item, UnknownPackagesAll unknownPackagesAll, string operationType, HttpResponseMessage responseMessage,string dryRunSuffix)
        {

            if (item.ComponentType == "NPM")
            {

                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "PYTHON")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesConan.Add(components);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetPackageinfo(item, operationType, responseMessage, dryRunSuffix);
                unknownPackagesAll.JfrogFoundPackagesDebian.Add(components);
            }

        }
        private static async Task SucessfullPackagesAsync(ComponentsToArtifactory item, UnknownPackagesAll unknownPackagesAll)
        {
           
            if (item.ComponentType == "NPM")
            {

                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.SuccessfullPackagesNpm.Add(components);
            }
            else if (item.ComponentType == "NUGET")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.SuccessfullPackagesNuget.Add(components);
            }
            else if (item.ComponentType == "MAVEN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.SuccessfullPackagesMaven.Add(components);
            }
            else if (item.ComponentType == "PYTHON")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.UnknownPackagesPython.Add(components);
            }
            else if (item.ComponentType == "CONAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.SuccessfullPackagesConan.Add(components);
            }
            else if(item.ComponentType == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetSucessFulPackageinfo(item);
                unknownPackagesAll.SuccessfullPackagesDebian.Add(components);
            }

        }

        private static PackageType GetPackageType(Component item)
        {
            string GetPropertyValue(string propertyName) =>
                    item.Properties
                        .Find(p => p.Name == propertyName)?
                        .Value?
                        .ToUpperInvariant();

            if (GetPropertyValue(Dataconstant.Cdx_ClearingState) == "APPROVED")
            {
                return PackageType.ClearedThirdParty;
            }
            else if (GetPropertyValue(Dataconstant.Cdx_IsInternal) == "TRUE")
            {
                return PackageType.Internal;
            }
            else if (GetPropertyValue(Dataconstant.Cdx_IsDevelopment) == "TRUE")
            {
                return PackageType.Development;
            }

            return PackageType.Unknown;
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
            else if (component.ComponentType == "PYTHON")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiCompName}";
            }
            else if (component.ComponentType == "CONAN")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
                // Add a wild card to the path end for jFrog AQL query search
                component.Path = $"{component.Path}/*";
            }
            else if (component.ComponentType == "DEBIAN")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*" +
                           $"?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*";
            }
            else
            {
                // Do nothing
            }
            return component.DryRun ? $"{url}&dry=1" : url;
        }

        private static string GetMoveURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == "NPM")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Name}/-/{component.PackageName}-{component.Version}" +
              $"{ApiConstant.NpmExtension}?to=/{component.DestRepoName}/{component.Name}/-/{component.PackageName}-{component.Version}{ApiConstant.NpmExtension}";
            }
            else if (component.ComponentType == "NUGET")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}" +
               $"{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == "MAVEN")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Name}/{component.Version}" +
               $"?to=/{component.DestRepoName}/{component.Name}/{component.Version}";
            }
            else if (component.ComponentType == "PYTHON")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiCompName}";
            }
            else if (component.ComponentType == "CONAN")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
                // Add a wild card to the path end for jFrog AQL query search
                component.Path = $"{component.Path}/*";
            }
            else if (component.ComponentType == "DEBIAN")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*" +
                          $"?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*";
            }
            else
            {
                // Do nothing
            }
            return component.DryRun ? $"{url}&dry=1" : url;
        }

        private static string GetPackagePath(ComponentsToArtifactory component, AqlResult aqlResult)
        {
            switch (component.ComponentType)
            {
                case "NPM":
                    return $"{component.Name}/-";

                case "CONAN" when aqlResult != null:
                    string path = aqlResult.Path;
                    string package = $"{component.Name}/{component.Version}";

                    if (path.Contains(package))
                    {
                        int index = path.IndexOf(package);
                        return path.Substring(0, index + package.Length);
                    }
                    else
                    {
                        return path;
                    }

                case "MAVEN":
                    return $"{component.Name}/{component.Version}";

                case "DEBIAN":
                    return $"pool/main/{component.Name[0]}/{component.Name}";
                default:
                    return string.Empty;
            }
        }

        private static string GetJfrogPackageName(ComponentsToArtifactory component)
        {
            string packageName;

            switch (component.ComponentType)
            {
                case "NPM":
                    packageName = $"{component.PackageName}-{component.Version}{ApiConstant.NpmExtension}";
                    break;

                case "NUGET":
                    packageName = $"{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}";
                    break;

                case "DEBIAN":
                    packageName = $"{component.PackageName}_{component.Version.Replace(ApiConstant.DebianExtension, "") + "*"}";
                    break;

                case "PYTHON":
                    packageName = component.PypiCompName;
                    break;

                default:
                    packageName = string.Empty;
                    break;
            }

            return packageName;
        }

        private static string GetDestinationRepo(Component item, CommonAppSettings appSettings)
        {
            var packageType = GetPackageType(item);
            var componentType = GetComponentType(item);

            if (!string.IsNullOrEmpty(componentType))
            {
                switch (componentType.ToLower())
                {
                    case "npm":
                        return GetRepoName(packageType, appSettings.Npm.JfrogInternalDestRepoName, appSettings.Npm.JfrogDevDestRepoName, appSettings.Npm.JfrogThirdPartyDestRepoName);
                    case "nuget":
                        return GetRepoName(packageType, appSettings.Nuget.JfrogInternalDestRepoName, appSettings.Nuget.JfrogDevDestRepoName, appSettings.Nuget.JfrogThirdPartyDestRepoName);
                    case "maven":
                        return GetRepoName(packageType, appSettings.Maven.JfrogInternalDestRepoName, appSettings.Maven.JfrogDevDestRepoName, appSettings.Maven.JfrogThirdPartyDestRepoName);
                    case "python":
                        return GetRepoName(packageType, appSettings.Python.JfrogInternalDestRepoName, appSettings.Python.JfrogDevDestRepoName, appSettings.Python.JfrogThirdPartyDestRepoName);
                    case "conan":
                        return GetRepoName(packageType, appSettings.Conan.JfrogInternalDestRepoName, appSettings.Conan.JfrogDevDestRepoName, appSettings.Conan.JfrogThirdPartyDestRepoName);
                    case "debian":
                        return GetRepoName(packageType, appSettings.Debian.JfrogInternalDestRepoName, appSettings.Debian.JfrogDevDestRepoName, appSettings.Debian.JfrogThirdPartyDestRepoName);
                }
            }

            return string.Empty;
        }

        private static string GetRepoName(PackageType packageType, string internalRepo, string developmentRepo, string clearedThirdPartyRepo)
        {
            switch (packageType)
            {
                case PackageType.Internal:
                    return internalRepo;
                case PackageType.Development:
                    return developmentRepo;
                case PackageType.ClearedThirdParty:
                    return clearedThirdPartyRepo;
                default:
                    return string.Empty;
            }
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
            else if (item.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                return "PYTHON";
            }
            else if (item.Purl.Contains("conan", StringComparison.OrdinalIgnoreCase))
            {
                return "CONAN";
            }
            else if (item.Purl.Contains("pkg:deb/debian", StringComparison.OrdinalIgnoreCase))
            {
                return "DEBIAN";
            }
            else
            {
                // Do nothing
            }
            return string.Empty;
        }

        private async static Task<AqlResult> GetSrcRepoDetailsForPyPiOrConanPackages(Component item)
        {
            if (item.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                // get the  component list from Jfrog for given repo
                aqlResultList = await GetListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoUrl)?.Value }, jFrogService);
                if (aqlResultList.Count > 0)
                {
                    return GetArtifactoryRepoName(aqlResultList, item);
                }
            }
            else if (item.Purl.Contains("conan", StringComparison.OrdinalIgnoreCase))
            {
                var aqlConanResultList = await GetListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoUrl)?.Value }, jFrogService);

                if (aqlConanResultList.Count > 0)
                {
                    return GetArtifactoryRepoNameForConan(aqlConanResultList, item);
                }
            }

            return null;
        }

        public static async Task UploadingThePackages(List<ComponentsToArtifactory> componentsToUpload, int timeout, UnknownPackagesAll unknownPackagesAll)
        {
            Logger.Debug("Starting UploadingThePackages() method");
            foreach (var item in componentsToUpload)
            {
                await PackageUploadToArtifactory(PackageUploader.uploaderKpiData, item, timeout,unknownPackagesAll);
            }

            if (SetWarningCode)
            {
                Environment.ExitCode = 2;
                Logger.Debug("Setting ExitCode to 2");
            }

            Logger.Debug("Ending UploadingThePackages() method");
            Program.UploaderStopWatch?.Stop();
        }

        private static async Task PackageUploadToArtifactory(UploaderKpiData uploaderKpiData, ComponentsToArtifactory item, int timeout,UnknownPackagesAll unknownPackagesAll)
        {
            var packageType = item.PackageType;
           
            if (!(item.SrcRepoName.Equals(item.DestRepoName, StringComparison.OrdinalIgnoreCase)) && !item.SrcRepoName.Contains("siparty-release"))
            {
                
                if (!(item.SrcRepoName.Contains("Not Found in JFrog")))
                {
                    const string dryRunSuffix = null;
                    string operationType = item.PackageType == PackageType.ClearedThirdParty || item.PackageType == PackageType.Development ? "copy" : "move";
                    ArtfactoryUploader.jFrogService = jFrogService;
                    HttpResponseMessage responseMessage = await ArtfactoryUploader.UploadPackageToRepo(item, timeout,unknownPackagesAll);

                    if (responseMessage.StatusCode == HttpStatusCode.OK && !item.DryRun)
                    {
                        IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, true);
                    }
                    else if (responseMessage.ReasonPhrase == ApiConstant.PackageNotFound)
                    {
                        await JfrogFoundPackagesAsync(item, unknownPackagesAll, operationType, responseMessage, dryRunSuffix);
                        IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, false);
                        item.DestRepoName = null;
                        SetWarningCode = true;
                    }
                    else if (responseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
                    {
                        await JfrogFoundPackagesAsync(item, unknownPackagesAll, operationType, responseMessage, dryRunSuffix);
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
                else
                {
                    uploaderKpiData.PackagesNotExistingInRemoteCache++;
                    item.DestRepoName = null;
                    await JfrogNotFoundPackagesAsync(item,unknownPackagesAll);
                    
                }
            }
            else
            {
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, true);
                await SucessfullPackagesAsync(item, unknownPackagesAll);                
                item.DestRepoName = null;
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
                { "Artifactory Uploader",uploaderKpiData.TimeTakenByComponentCreator }
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

        private static async Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var test = await jFrogService.GetInternalComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(test);
                }
            }

            return aqlResultList;
        }

        private static AqlResult GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}";

            AqlResult repoName = aqlResultList.Find(x => x.Name.Contains(jfrogcomponentName, StringComparison.OrdinalIgnoreCase));

            return repoName;
        }

        private static AqlResult GetArtifactoryRepoNameForConan(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";

            AqlResult repoName = aqlResultList.Find(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            return repoName;
        }

        public static void UpdateBomArtifactoryRepoUrl(ref Bom bom, List<ComponentsToArtifactory> componentsUploaded)
        {
            foreach (var component in componentsUploaded)
            {
                var bomComponent = bom.Components.Find(x => x.Purl.Equals(component.Purl, StringComparison.OrdinalIgnoreCase));
                if (component.DestRepoName != null && !component.DryRun)
                {
                    bomComponent.Properties.First(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoUrl).Value = component.DestRepoName;
                }
            }
        }

    }

}
