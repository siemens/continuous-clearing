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

        public async static Task<List<ComponentsToArtifactory>> GetComponentsToBeUploadedToArtifactory(List<Component> comparisonBomData,
                                                                                                       CommonAppSettings appSettings,
                                                                                                       DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("Starting GetComponentsToBeUploadedToArtifactory() method");
            List<ComponentsToArtifactory> componentsToBeUploaded = new List<ComponentsToArtifactory>();

            foreach (var item in comparisonBomData)
            {
                var packageType = GetPackageType(item);
                if (packageType != PackageType.Unknown)
                {
                    AqlResult aqlResult = await GetSrcRepoDetailsForComponent(item);
                    ComponentsToArtifactory components = new ComponentsToArtifactory()
                    {
                        Name = !string.IsNullOrEmpty(item.Group) ? $"{item.Group}/{item.Name}" : item.Name,
                        PackageName = item.Name,
                        Version = item.Version,
                        Purl = item.Purl,
                        ComponentType = GetComponentType(item),
                        PackageType = packageType,
                        DryRun = appSettings.Jfrog.DryRun,
                        SrcRepoName = item.Properties.Find(s => s.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value,
                        DestRepoName = GetDestinationRepo(item, appSettings),
                        Token = appSettings.Jfrog.Token,
                        JfrogApi = appSettings.Jfrog.URL
                    };

                    if (aqlResult != null)
                    {
                        components.SrcRepoPathWithFullName = aqlResult.Repo + "/" + aqlResult.Path + "/" + aqlResult.Name;
                        components.PypiOrNpmCompName = aqlResult.Name;
                    }
                    else
                    {
                        components.SrcRepoPathWithFullName = string.Empty;
                        components.PypiOrNpmCompName = string.Empty;
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
                    await PackageUploadInformation.AddUnknownPackagesAsync(item, displayPackagesInfo);
                }
            }
            Logger.Debug("Ending GetComponentsToBeUploadedToArtifactory() method");
            return componentsToBeUploaded;
        }

        private static PackageType GetPackageType(Component item)
        {
            string GetPropertyValue(string propertyName) =>
                item.Properties
                    .Find(p => p.Name == propertyName)?
                    .Value?
                    .ToUpperInvariant();

            var propertyChecks = new Dictionary<string, PackageType>
    {
        { Dataconstant.Cdx_ClearingState, PackageType.ClearedThirdParty },
        { Dataconstant.Cdx_IsInternal, PackageType.Internal },
        { Dataconstant.Cdx_IsDevelopment, PackageType.Development }
    };

            foreach (var check in propertyChecks)
            {
                if (GetPropertyValue(check.Key) == "TRUE" || (check.Key == Dataconstant.Cdx_ClearingState && GetPropertyValue(check.Key) == "APPROVED"))
                {
                    return check.Value;
                }
            }

            return PackageType.Unknown;
        }
        public static string GetCopyURL(ComponentsToArtifactory component)
        {
            string url = component.ComponentType switch
            {
                "NPM" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}",
                "NUGET" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}",
                "MAVEN" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Name}/{component.Version}?to=/{component.DestRepoName}/{component.Name}/{component.Version}",
                "POETRY" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}",
                "CONAN" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}?to=/{component.DestRepoName}/{component.Path}",
                "DEBIAN" => $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*",
                _ => string.Empty
            };

            if (component.ComponentType == "CONAN")
            {
                component.Path = $"{component.Path}/*";
            }

            return component.DryRun ? $"{url}&dry=1" : url;
        }

        public static string GetMoveURL(ComponentsToArtifactory component)
        {
            string url = component.ComponentType switch
            {
                "NPM" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}",
                "NUGET" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}",
                "MAVEN" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Name}/{component.Version}?to=/{component.DestRepoName}/{component.Name}/{component.Version}",
                "POETRY" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}",
                "CONAN" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}?to=/{component.DestRepoName}/{component.Path}",
                "DEBIAN" => $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*",
                _ => string.Empty
            };

            if (component.ComponentType == "CONAN")
            {
                component.Path = $"{component.Path}/*";
            }

            return component.DryRun ? $"{url}&dry=1" : url;
        }

        private static string GetPackagePath(ComponentsToArtifactory component, AqlResult aqlResult)
        {
            return component.ComponentType switch
            {
                "NPM" => aqlResult != null ? aqlResult.Path : $"{component.Name}/-",
                "CONAN" when aqlResult != null => GetConanPackagePath(aqlResult.Path, component.Name, component.Version),
                "MAVEN" => $"{component.Name}/{component.Version}",
                "DEBIAN" => $"pool/main/{component.Name[0]}/{component.Name}",
                _ => string.Empty
            };
        }

        private static string GetConanPackagePath(string path, string name, string version)
        {
            string package = $"{name}/{version}";
            if (path.Contains(package))
            {
                int index = path.IndexOf(package);
                return path.Substring(0, index + package.Length);
            }
            return path;
        }
        private static string GetJfrogPackageName(ComponentsToArtifactory component)
        {
            var packageNameFormats = new Dictionary<string, Func<ComponentsToArtifactory, string>>
    {
        { "NPM", c => c.PypiOrNpmCompName },
        { "NUGET", c => $"{c.PackageName}.{c.Version}{ApiConstant.NugetExtension}" },
        { "DEBIAN", c => $"{c.PackageName}_{c.Version.Replace(ApiConstant.DebianExtension, "") + "*"}" },
        { "POETRY", c => c.PypiOrNpmCompName }
    };

            return packageNameFormats.TryGetValue(component.ComponentType, out var formatFunc) ? formatFunc(component) : string.Empty;
        }

        private static string GetDestinationRepo(Component item, CommonAppSettings appSettings)
        {
            var packageType = GetPackageType(item);
            var componentType = GetComponentType(item);

            if (string.IsNullOrEmpty(componentType))
            {
                return string.Empty;
            }

            var repoMappings = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "npm", () => GetRepoName(packageType, appSettings.Npm.ReleaseRepo, appSettings.Npm.DevDepRepo, appSettings.Npm.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) },
        { "nuget", () => GetRepoName(packageType, appSettings.Nuget.ReleaseRepo, appSettings.Nuget.DevDepRepo, appSettings.Nuget.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) },
        { "maven", () => GetRepoName(packageType, appSettings.Maven.ReleaseRepo, appSettings.Maven.DevDepRepo, appSettings.Maven.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) },
        { "poetry", () => GetRepoName(packageType, appSettings.Poetry.ReleaseRepo, appSettings.Poetry.DevDepRepo, appSettings.Poetry.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) },
        { "conan", () => GetRepoName(packageType, appSettings.Conan.ReleaseRepo, appSettings.Conan.DevDepRepo, appSettings.Conan.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) },
        { "debian", () => GetRepoName(packageType, appSettings.Debian.ReleaseRepo, appSettings.Debian.DevDepRepo, appSettings.Debian.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name) }
    };

            return repoMappings.TryGetValue(componentType, out var getRepoName) ? getRepoName() : string.Empty;
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
            var componentTypeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "npm", "NPM" },
        { "nuget", "NUGET" },
        { "maven", "MAVEN" },
        { "pypi", "POETRY" },
        { "conan", "CONAN" },
        { "pkg:deb/debian", "DEBIAN" }
    };

            foreach (var mapping in componentTypeMappings)
            {
                if (item.Purl.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.Value;
                }
            }

            return string.Empty;
        }

        public async static Task<AqlResult> GetSrcRepoDetailsForComponent(Component item)
        {
            if (item.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                // get the  component list from Jfrog for given repo
                aqlResultList = await GetPypiListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, jFrogService);
                if (aqlResultList.Count > 0)
                {
                    return GetArtifactoryRepoName(aqlResultList, item);
                }
            }
            else if (item.Purl.Contains("conan", StringComparison.OrdinalIgnoreCase))
            {
                var aqlConanResultList = await GetListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, jFrogService);

                if (aqlConanResultList.Count > 0)
                {
                    return GetArtifactoryRepoNameForConan(aqlConanResultList, item);
                }
            }
            else if (item.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                aqlResultList = await GetNpmListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, jFrogService);

                if (aqlResultList.Count > 0)
                {
                    return GetNpmArtifactoryRepoName(aqlResultList, item);
                }
            }

            return null;
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
                    await PackageUploadInformation.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);
                }
            }
            else
            {
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, true);
                await PackageUploadInformation.SucessfullPackagesAsync(item, displayPackagesInfo);
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
                await PackageUploadInformation.JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);
                IncrementCountersBasedOnPackageType(uploaderKpiData, packageType, false);
                item.DestRepoName = null;
                SetWarningCode = true;
            }
            else if (responseMessage.ReasonPhrase == ApiConstant.ErrorInUpload)
            {
                await PackageUploadInformation.JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);
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

        public static async Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
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
        public static async Task<List<AqlResult>> GetPypiListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var componentRepoData = await jFrogService.GetPypiComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(componentRepoData);
                }
            }

            return aqlResultList;
        }

        public static async Task<List<AqlResult>> GetNpmListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var componentRepoData = await jFrogService.GetNpmComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(componentRepoData);
                }
            }

            return aqlResultList;
        }

        private static AqlResult GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogpackageName = GetFullNameOfComponent(component);
            return aqlResultList.Find(x => x.Properties != null &&
                                  x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == jfrogpackageName) &&
                                  x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version));
        }

        private static AqlResult GetNpmArtifactoryRepoName(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogpackageName = GetFullNameOfComponent(component);
            return aqlResultList.Find(x => x.Properties != null &&
                                   x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) &&
                                   x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));
        }


        private static AqlResult GetArtifactoryRepoNameForConan(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";

            AqlResult repoName = aqlResultList.Find(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            return repoName;
        }

        private static string GetFullNameOfComponent(Component item)
        {
            if (!string.IsNullOrEmpty(item.Group))
            {
                return $"{item.Group}/{item.Name}";
            }
            else
            {
                return item.Name;
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

        internal static async Task<Bom> UpdateJfrogRepoPathForSucessfullyUploadedItems(Bom m_ComponentsInBOM,
                                                                            DisplayPackagesInfo displayPackagesInfo)
        {
            // Get details of sucessfully uploaded packages
            List<ComponentsToArtifactory> uploadedPackages = GetUploadePackageDetails(displayPackagesInfo);

            // Get the details of all the dest repo names from jfrog at once
            List<string> destRepoNames = uploadedPackages.Select(x => x.DestRepoName)?.Distinct()?.ToList() ?? new List<string>();
            List<AqlResult> jfrogPackagesListAql = await GetJfrogRepoInfoForAllTypePackages(destRepoNames);

            // Update the repo path
            List<Component> bomComponents = UpdateJfroRepoPathProperty(m_ComponentsInBOM, uploadedPackages, jfrogPackagesListAql);
            m_ComponentsInBOM.Components = bomComponents;
            return m_ComponentsInBOM;

        }

        private static List<Component> UpdateJfroRepoPathProperty(Bom m_ComponentsInBOM,
                                                                  List<ComponentsToArtifactory> uploadedPackages,
                                                                  List<AqlResult> jfrogPackagesListAql)
        {
            List<Component> bomComponents = m_ComponentsInBOM.Components;
            foreach (var component in bomComponents)
            {
                // check component exists in upload list
                var package = uploadedPackages.FirstOrDefault(x => x.Name.Contains($"{component.Name}")
                 && x.Version.Contains($"{component.Version}") && x.Purl.Contains(component.Purl));

                // if component not exists in upload list move to nect item in the loop
                if (package == null) { continue; }

                // get jfrog details of a component from the aqlresult set
                string packageNameEXtension = GetPackageNameExtensionBasedOnComponentType(package);
                AqlResult jfrogData = GetJfrogInfoOfThePackageUploaded(jfrogPackagesListAql, package, packageNameEXtension);

                // if package not exists in jfrog list move to nect item in the loop
                if (jfrogData == null) { continue; }

                // Get path and update the component with new repo path property
                string newRepoPath = GetJfrogRepoPath(jfrogData) ?? Dataconstant.JfrogRepoPathNotFound;
                Property repoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = newRepoPath };
                if (component.Properties == null)
                {
                    component.Properties = new List<Property> { };
                    component.Properties.Add(repoPathProperty);
                    continue;
                }

                if (component.Properties.Exists(x => x.Name.Equals(Dataconstant.Cdx_JfrogRepoPath, StringComparison.OrdinalIgnoreCase)))
                {
                    component
                        .Properties
                        .Find(x => x.Name.Equals(Dataconstant.Cdx_JfrogRepoPath, StringComparison.OrdinalIgnoreCase))
                        .Value = newRepoPath;
                    continue;
                }

                // if repo path property not exists
                component.Properties.Add(repoPathProperty);
            }

            return bomComponents;
        }

        private static AqlResult GetJfrogInfoOfThePackageUploaded(List<AqlResult> jfrogPackagesListAql, ComponentsToArtifactory package, string packageNameEXtension)
        {
            string pkgType = package.ComponentType ?? string.Empty;
            if (pkgType.Equals("CONAN", StringComparison.OrdinalIgnoreCase))
            {
                return jfrogPackagesListAql.FirstOrDefault(x => x.Path.Contains(package.Name)
                                                 && x.Path.Contains(package.Version)
                                                 && x.Name.Contains($"package.{packageNameEXtension}"));
            }
            return jfrogPackagesListAql.FirstOrDefault(x => x.Path.Contains(package.Name)
                                                 && x.Name.Contains(package.Version)
                                                 && x.Name.Contains(packageNameEXtension));
        }

        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }
            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        public static string GetPackageNameExtensionBasedOnComponentType(ComponentsToArtifactory package)
        {
            var packageExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", ".tgz" },
        { "NUGET", ".nupkg" },
        { "MAVEN", ".jar" },
        { "DEBIAN", ".deb" },
        { "POETRY", ".whl" },
        { "CONAN", "package.tgz" }
    };

            return packageExtensions.TryGetValue(package.ComponentType, out var extension) ? extension : string.Empty;
        }

        public static async Task<List<AqlResult>> GetJfrogRepoInfoForAllTypePackages(List<string> destRepoNames)
        {
            if (destRepoNames != null && destRepoNames.Count > 0)
            {
                foreach (var repo in destRepoNames)
                {
                    var result = await jFrogService.GetInternalComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(result);
                }
            }

            return aqlResultList;
        }

        public static List<ComponentsToArtifactory> GetUploadePackageDetails(DisplayPackagesInfo displayPackagesInfo)
        {
            List<ComponentsToArtifactory> uploadedPackages = new List<ComponentsToArtifactory>();

            var allPackages = new List<IEnumerable<ComponentsToArtifactory>>
    {
        displayPackagesInfo.JfrogFoundPackagesConan,
        displayPackagesInfo.JfrogFoundPackagesMaven,
        displayPackagesInfo.JfrogFoundPackagesNpm,
        displayPackagesInfo.JfrogFoundPackagesNuget,
        displayPackagesInfo.JfrogFoundPackagesPython,
        displayPackagesInfo.JfrogFoundPackagesDebian
    };

            foreach (var packageList in allPackages)
            {
                foreach (var item in packageList)
                {
                    if (item.ResponseMessage?.StatusCode == HttpStatusCode.OK)
                    {
                        uploadedPackages.Add(item);
                    }
                }
            }

            return uploadedPackages;
        }
    }

}
