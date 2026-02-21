// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Jfrog

using ArtifactoryUploader.Constants;
using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    public static class UploadToArtifactory
    {
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<string, IList<AqlResult>> repoCache = new();
        private const string Choco = "CHOCO";
        private const string Nuget = "NUGET";
        private const string Maven = "MAVEN";
        private const string Poetry = "POETRY";
        private const string Conan = "CONAN";
        private const string Cargo = "CARGO";
        private const string Debian = "DEBIAN";
        private const string NPM = "NPM";

        #endregion

        #region Properties

        public static IJFrogService JFrogService { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously retrieves the list of components to be uploaded to Artifactory.
        /// </summary>
        /// <param name="comparisonBomData">The list of components from the BOM data.</param>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="displayPackagesInfo">The display information for packages.</param>
        /// <returns>A list of components prepared for upload to Artifactory.</returns>
        public async static Task<List<ComponentsToArtifactory>> GetComponentsToBeUploadedToArtifactory(List<Component> comparisonBomData,
                                                                                                      CommonAppSettings appSettings,
                                                                                                      DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("GetComponentsToBeUploadedToArtifactory():Starting to get component data for upload to artifactory");
            List<ComponentsToArtifactory> componentsToBeUploaded = new List<ComponentsToArtifactory>();

            foreach (var item in comparisonBomData)
            {
                Logger.DebugFormat("GetComponentsToBeUploadedToArtifactory(): Identifying data for this component name-{0}, version-{1}", item.Name, item.Version);
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
                        components.SrcRepoPathWithFullName = aqlResult.Repo + Dataconstant.ForwardSlash + aqlResult.Path + Dataconstant.ForwardSlash + aqlResult.Name;
                        components.PypiOrNpmCompName = aqlResult.Name;
                    }
                    else
                    {
                        components.SrcRepoPathWithFullName = string.Empty;
                        components.PypiOrNpmCompName = string.Empty;
                    }

                    components.Path = GetPackagePath(components, aqlResult, item);
                    components.CopyPackageApiUrl = GetCopyURL(components);
                    components.MovePackageApiUrl = GetMoveURL(components);
                    components.JfrogPackageName = GetJfrogPackageName(components);
                    components.JfrogRepoPath = GetJfrogRepPath(components);
                    componentsToBeUploaded.Add(components);
                    Logger.DebugFormat("GetComponentsToBeUploadedToArtifactory(): Component identified as unknown package type, name-{0}, version-{1}", item.Name, item.Version);
                }
                else
                {
                    PackageUploader.uploaderKpiData.ComponentNotApproved++;
                    PackageUploader.uploaderKpiData.PackagesNotUploadedToJfrog++;
                    await AddUnknownPackagesAsync(item, displayPackagesInfo);
                }
            }
            ValidComponentsIdentifiedToBeUpload(componentsToBeUploaded);
            Logger.Debug("GetComponentsToBeUploadedToArtifactory():Completed to getting component data for upload to artifactory");
            return componentsToBeUploaded;
        }
        private static void ValidComponentsIdentifiedToBeUpload(List<ComponentsToArtifactory> componentsToBeUploaded)
        {
            if (componentsToBeUploaded == null || componentsToBeUploaded.Count == 0)
            {
                Logger.Debug("No components to be uploaded to Artifactory.");
                return;
            }

            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n================================================================================================================");
            logBuilder.AppendLine(" Components to be Uploaded to Artifactory");
            logBuilder.AppendLine("================================================================================================================");
            logBuilder.AppendLine($"| {"Name",-50} | {"Version",-15} | {"ComponentType",-15} | {"PackageType",-20} | {"SrcRepoName",-20} | {"DestRepoName",-20} |");
            logBuilder.AppendLine("----------------------------------------------------------------------------------------------------------------");

            foreach (var component in componentsToBeUploaded)
            {
                logBuilder.AppendLine($"| {component.Name,-50} | {component.Version,-15} | {component.ComponentType,-15} | {component.PackageType,-20} | {component.SrcRepoName,-20} | {component.DestRepoName,-20} |");
            }

            logBuilder.AppendLine("================================================================================================================");

            Logger.Debug(logBuilder.ToString());
        }
        private static string GetComponentType(Component item)
        {
            var projectTypeProp = item.Properties
                                       ?.Find(p => p.Name == Dataconstant.Cdx_ProjectType)
                                       ?.Value;
            if (!string.IsNullOrEmpty(projectTypeProp) &&
                projectTypeProp.Equals("choco", StringComparison.InvariantCultureIgnoreCase))
            {
                return Choco;
            }
            if (item.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                return NPM;
            }
            else if (item.Purl.Contains("nuget", StringComparison.OrdinalIgnoreCase))
            {
                return Nuget;
            }
            else if (item.Purl.Contains("maven", StringComparison.OrdinalIgnoreCase))
            {
                return Maven;
            }
            else if (item.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                return Poetry;
            }
            else if (item.Purl.Contains("conan", StringComparison.OrdinalIgnoreCase))
            {
                return Conan;
            }
            else if (item.Purl.Contains("pkg:deb/debian", StringComparison.OrdinalIgnoreCase))
            {
                return Debian;
            }
            else if (item.Purl.Contains(ArtifactoryConstant.Cargo, StringComparison.OrdinalIgnoreCase))
            {
                return Cargo;
            }
            else
            {
                // Do nothing
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the JFrog repository path for the specified component.
        /// </summary>
        /// <param name="component">The component to generate the path for.</param>
        /// <returns>The JFrog repository path.</returns>
        public static string GetJfrogRepPath(ComponentsToArtifactory component)
        {
            string jfrogRepPath = string.Empty;
            if (component.ComponentType == NPM)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}";
            }
            else if (component.ComponentType == Nuget)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == Maven)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Name}/{component.Version}";
            }
            else if (component.ComponentType == Poetry)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.PypiOrNpmCompName}";
            }
            else if (component.ComponentType == Conan)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Path}";
            }
            else if (component.ComponentType == Debian)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*";
            }
            else if (component.ComponentType == Cargo)
            {
                jfrogRepPath = $"{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.CargoExtension}";
            }
            else
            {
                // Do nothing
            }
            return jfrogRepPath;
        }

        /// <summary>
        /// Gets the destination repository name based on the component and application settings.
        /// </summary>
        /// <param name="item">The component to evaluate.</param>
        /// <param name="appSettings">The common application settings.</param>
        /// <returns>The destination repository name.</returns>
        private static string GetDestinationRepo(Component item, CommonAppSettings appSettings)
        {
            var packageType = GetPackageType(item);
            var componentType = GetComponentType(item);

            if (!string.IsNullOrEmpty(componentType))
            {
                switch (componentType.ToLower())
                {
                    case "npm":
                        return GetRepoName(packageType, appSettings.Npm.ReleaseRepo, appSettings.Npm.DevDepRepo, appSettings.Npm.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "nuget":
                        return GetRepoName(packageType, appSettings.Nuget.ReleaseRepo, appSettings.Nuget.DevDepRepo, appSettings.Nuget.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "choco":
                        return GetRepoName(packageType, appSettings.Choco.ReleaseRepo, appSettings.Choco.DevDepRepo, appSettings.Choco.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "maven":
                        return GetRepoName(packageType, appSettings.Maven.ReleaseRepo, appSettings.Maven.DevDepRepo, appSettings.Maven.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "poetry":
                        return GetRepoName(packageType, appSettings.Poetry.ReleaseRepo, appSettings.Poetry.DevDepRepo, appSettings.Poetry.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "conan":
                        return GetRepoName(packageType, appSettings.Conan.ReleaseRepo, appSettings.Conan.DevDepRepo, appSettings.Conan.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case "debian":
                        return GetRepoName(packageType, appSettings.Debian.ReleaseRepo, appSettings.Debian.DevDepRepo, appSettings.Debian.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                    case ArtifactoryConstant.Cargo:
                        return GetRepoName(packageType, appSettings.Cargo.ReleaseRepo, appSettings.Cargo.DevDepRepo, appSettings.Cargo.Artifactory.ThirdPartyRepos.FirstOrDefault(x => x.Upload)?.Name);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the package path for the specified component.
        /// </summary>
        /// <param name="component">The component to generate the path for.</param>
        /// <param name="aqlResult">The AQL query result.</param>
        /// <param name="item">The source component.</param>
        /// <returns>The package path.</returns>
        private static string GetPackagePath(ComponentsToArtifactory component, AqlResult aqlResult, Component item)
        {
            switch (component.ComponentType)
            {
                case NPM:
                    if (aqlResult != null)
                    {
                        return $"{aqlResult.Path}";
                    }
                    else
                    {
                        return $"{component.Name}/-";
                    }

                case Conan when aqlResult != null:
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

                case Maven:
                    string groupWithSlash = !string.IsNullOrEmpty(item.Group) ? item.Group.Replace('.', '/') : string.Empty;
                    string mavenPath = !string.IsNullOrEmpty(groupWithSlash) ? $"{groupWithSlash}/{item.Name}" : item.Name;
                    return $"{mavenPath}/{component.Version}";

                case Debian:
                    return $"pool/main/{component.Name[0]}/{component.Name}";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the copy API URL for copying the component to the destination repository.
        /// </summary>
        /// <param name="component">The component to generate the URL for.</param>
        /// <returns>The copy API URL.</returns>
        public static string GetCopyURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == NPM)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}";

            }
            else if (component.ComponentType == Nuget || component.ComponentType == Choco)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}" +
               $"{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == Maven)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
            }
            else if (component.ComponentType == Poetry)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
            }
            else if (component.ComponentType == Conan)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
                // Add a wild card to the path end for jFrog AQL query search
                component.Path = $"{component.Path}/*";
            }
            else if (component.ComponentType == Debian)
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*" +
                           $"?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*";
            }
            else if (component.ComponentType == Cargo)
            {
                //Copy to Destination Repo, not keeping the folder structure intact
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
            }
            else
            {
                // Do nothing
            }
            return component.DryRun ? $"{url}&dry=1" : url;
        }

        /// <summary>
        /// Gets the move API URL for moving the component to the destination repository.
        /// </summary>
        /// <param name="component">The component to generate the URL for.</param>
        /// <returns>The move API URL.</returns>
        public static string GetMoveURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == NPM)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
              $"?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}";

            }
            else if (component.ComponentType == Nuget || component.ComponentType == Choco)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.PackageName}.{component.Version}" +
               $"{ApiConstant.NugetExtension}?to=/{component.DestRepoName}/{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            }
            else if (component.ComponentType == Maven)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
            }
            else if (component.ComponentType == Poetry)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
            }
            else if (component.ComponentType == Conan)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}" +
               $"?to=/{component.DestRepoName}/{component.Path}";
                // Add a wild card to the path end for jFrog AQL query search
                component.Path = $"{component.Path}/*";
            }
            else if (component.ComponentType == Debian)
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*" +
                          $"?to=/{component.DestRepoName}/{component.Path}/{component.Name}_{component.Version.Replace(ApiConstant.DebianExtension, "")}*";
            }
            else if (component.ComponentType == Cargo)
            {
                //Move to Destination Repo, not keeping the folder structure intact
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
            }
            else
            {
                // Do nothing
            }
            return component.DryRun ? $"{url}&dry=1" : url;
        }

        /// <summary>
        /// Gets the JFrog package name for the specified component.
        /// </summary>
        /// <param name="component">The component to generate the package name for.</param>
        /// <returns>The JFrog package name.</returns>
        private static string GetJfrogPackageName(ComponentsToArtifactory component)
        {
            return component.ComponentType switch
            {
                NPM => component.PypiOrNpmCompName,
                Nuget => $"{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}",
                Choco => $"{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}",
                Debian => $"{component.PackageName}_{component.Version.Replace(ApiConstant.DebianExtension, "") + "*"}",
                Cargo => $"{component.PackageName}.{component.Version}{ApiConstant.CargoExtension}",
                Poetry => component.PypiOrNpmCompName,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Asynchronously adds unknown packages to the display packages information.
        /// </summary>
        /// <param name="item">The component to add as an unknown package.</param>
        /// <param name="displayPackagesInfo">The display information for packages.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task AddUnknownPackagesAsync(Component item, DisplayPackagesInfo displayPackagesInfo)
        {
            string GetPropertyValue(string propertyName) =>
                  item.Properties
                      .Find(p => p.Name == propertyName)?
                      .Value?
                      .ToUpperInvariant();

            string projectType = GetPropertyValue(Dataconstant.Cdx_ProjectType);
            ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
            AddUnknownComponentToDisplayList(projectType, components, displayPackagesInfo);
        }

        /// <summary>
        /// Adds an unknown component to the appropriate display list based on project type.
        /// </summary>
        /// <param name="projectType">The type of the project.</param>
        /// <param name="component">The component to add.</param>
        /// <param name="displayPackagesInfo">The display information for packages.</param>
        private static void AddUnknownComponentToDisplayList(string projectType, ComponentsToArtifactory component, DisplayPackagesInfo displayPackagesInfo)
        {
            switch (projectType)
            {
                case NPM:
                    displayPackagesInfo.UnknownPackagesNpm.Add(component);
                    break;
                case Nuget:
                    displayPackagesInfo.UnknownPackagesNuget.Add(component);
                    break;
                case Maven:
                    displayPackagesInfo.UnknownPackagesMaven.Add(component);
                    break;
                case Poetry:
                    displayPackagesInfo.UnknownPackagesPython.Add(component);
                    break;
                case Conan:
                    displayPackagesInfo.UnknownPackagesConan.Add(component);
                    break;
                case Debian:
                    displayPackagesInfo.UnknownPackagesDebian.Add(component);
                    break;
                case Cargo:
                    displayPackagesInfo.UnknownPackagesCargo.Add(component);
                    break;
                case Choco:
                    displayPackagesInfo.UnknownPackagesChoco.Add(component);
                    break;
            }
        }

        /// <summary>
        /// Gets the unknown package information from a component.
        /// </summary>
        /// <param name="item">The component to extract information from.</param>
        /// <returns>A task containing the unknown package information.</returns>
        private static Task<ComponentsToArtifactory> GetUnknownPackageinfo(Component item)
        {

            ComponentsToArtifactory components = new ComponentsToArtifactory()
            {
                Name = item.Name,
                Version = item.Version
            };
            return Task.FromResult(components);

        }

        /// <summary>
        /// Gets the repository name based on the package type.
        /// </summary>
        /// <param name="packageType">The type of the package.</param>
        /// <param name="internalRepo">The internal repository name.</param>
        /// <param name="developmentRepo">The development repository name.</param>
        /// <param name="clearedThirdPartyRepo">The cleared third-party repository name.</param>
        /// <returns>The repository name corresponding to the package type.</returns>
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

        /// <summary>
        /// Asynchronously gets the source repository details for a component.
        /// </summary>
        /// <param name="item">The component to retrieve repository details for.</param>
        /// <returns>The AQL result containing repository details, or null if not found.</returns>
        public async static Task<AqlResult> GetSrcRepoDetailsForComponent(Component item)
        {
            if (item.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                // get the  component list from Jfrog for given repo
                var aqlResultList = await GetPypiListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, JFrogService);
                if (aqlResultList.Count > 0)
                {
                    return GetArtifactoryRepoName(aqlResultList, item);
                }
            }
            else if (item.Purl.Contains("conan", StringComparison.OrdinalIgnoreCase))
            {
                var aqlConanResultList = await GetListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, JFrogService);

                if (aqlConanResultList.Count > 0)
                {
                    return GetArtifactoryRepoNameForConan(aqlConanResultList, item);
                }
            }
            else if (item.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                var aqlResultList = await GetPackageTypeBased_ListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, JFrogService, "npm");

                if (aqlResultList.Count > 0)
                {
                    return GetArtifactoryRepoName(aqlResultList, item);
                }
            }
            else if (item.Purl.Contains(ArtifactoryConstant.Cargo, StringComparison.OrdinalIgnoreCase))
            {
                var aqlResultList = await GetPackageTypeBased_ListOfComponentsFromRepo(new string[] { item.Properties.Find(x => x.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value }, JFrogService, ArtifactoryConstant.Cargo);

                if (aqlResultList.Count > 0)
                {
                    return GetArtifactoryRepoName(aqlResultList, item);
                }
            }

            return null;
        }

        /// <summary>
        /// Asynchronously gets the list of components from repositories based on package type.
        /// </summary>
        /// <param name="repoList">The list of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <param name="packageType">The type of package (npm or cargo).</param>
        /// <returns>A list of AQL results for the specified package type.</returns>
        public static async Task<List<AqlResult>> GetPackageTypeBased_ListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService, string packageType)
        {
            var aqlResultList = new List<AqlResult>();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    if (repoCache.TryGetValue(repo, out IList<AqlResult> value))
                    {
                        aqlResultList.AddRange(value);
                    }
                    else
                    {
                        if (packageType == "npm")
                        {
                            var componentRepoData = await jFrogService.GetNpmComponentDataByRepo(repo) ?? new List<AqlResult>();
                            repoCache[repo] = componentRepoData;
                            aqlResultList.AddRange(componentRepoData);
                        }
                        else if (packageType == ArtifactoryConstant.Cargo)
                        {
                            var componentRepoData = await jFrogService.GetCargoComponentDataByRepo(repo) ?? new List<AqlResult>();
                            repoCache[repo] = componentRepoData;
                            aqlResultList.AddRange(componentRepoData);
                        }
                    }
                }
            }
            return aqlResultList;
        }

        /// <summary>
        /// Asynchronously gets the list of internal components from repositories.
        /// </summary>
        /// <param name="repoList">The list of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A list of AQL results for internal components.</returns>
        public static async Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            var aqlResultList = new List<AqlResult>();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    if (repoCache.TryGetValue(repo, out IList<AqlResult> value))
                    {
                        aqlResultList.AddRange(value);
                    }
                    else
                    {
                        var componentRepoData = await jFrogService.GetInternalComponentDataByRepo(repo) ?? new List<AqlResult>();
                        repoCache[repo] = componentRepoData;
                        aqlResultList.AddRange(componentRepoData);
                    }
                }
            }
            return aqlResultList;
        }

        /// <summary>
        /// Gets the Artifactory repository name by matching component properties.
        /// </summary>
        /// <param name="aqlResultList">The list of AQL results to search.</param>
        /// <param name="component">The component to find.</param>
        /// <returns>The matching AQL result, or an empty result if not found.</returns>
        private static AqlResult GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogpackageName = GetFullNameOfComponent(component);
            if (component.Purl.Contains("pypi", StringComparison.OrdinalIgnoreCase))
            {
                return aqlResultList.Find(x => x.Properties != null &&
                                      x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == jfrogpackageName) &&
                                      x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version));
            }
            else if (component.Purl.Contains("npm", StringComparison.OrdinalIgnoreCase))
            {
                return aqlResultList.Find(x => x.Properties != null &&
                                       x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) &&
                                       x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));
            }
            else if (component.Purl.Contains(ArtifactoryConstant.Cargo, StringComparison.OrdinalIgnoreCase))
            {
                return aqlResultList.Find(x => x.Properties != null &&
                                       x.Properties.Any(p => p.Key == "crate.name" && p.Value == jfrogpackageName) &&
                                       x.Properties.Any(p => p.Key == "crate.version" && p.Value == component.Version));
            }
            return new AqlResult();
        }

        /// <summary>
        /// Gets the full name of a component, including group if present.
        /// </summary>
        /// <param name="item">The component to get the full name for.</param>
        /// <returns>The full component name.</returns>
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

        /// <summary>
        /// Gets the Artifactory repository name for Conan packages.
        /// </summary>
        /// <param name="aqlResultList">The list of AQL results to search.</param>
        /// <param name="component">The component to find.</param>
        /// <returns>The matching AQL result, or null if not found.</returns>
        private static AqlResult GetArtifactoryRepoNameForConan(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";

            AqlResult repoName = aqlResultList.Find(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            return repoName;
        }

        /// <summary>
        /// Asynchronously gets the list of PyPI components from repositories.
        /// </summary>
        /// <param name="repoList">The list of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A list of AQL results for PyPI components.</returns>
        public static async Task<List<AqlResult>> GetPypiListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            var aqlResultList = new List<AqlResult>();

            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    if (repoCache.TryGetValue(repo, out IList<AqlResult> value))
                    {
                        aqlResultList.AddRange(value);
                    }
                    else
                    {
                        var componentRepoData = await jFrogService.GetPypiComponentDataByRepo(repo) ?? new List<AqlResult>();
                        repoCache[repo] = componentRepoData;
                        aqlResultList.AddRange(componentRepoData);
                    }
                }
            }
            return aqlResultList;
        }

        /// <summary>
        /// Gets the package type from the component's properties.
        /// </summary>
        /// <param name="item">The component to evaluate.</param>
        /// <returns>The package type.</returns>
        private static PackageType GetPackageType(Component item)
        {
            string GetPropertyValue(string propertyName) =>
                    item.Properties
                        .Find(p => p.Name == propertyName)?
                        .Value?
                        .ToUpperInvariant();
            Logger.DebugFormat("GetPackageType(): Determining package type for Component - Name: {0}, Version: {1}", item.Name, item.Version);
            if (GetPropertyValue(Dataconstant.Cdx_ClearingState) == "APPROVED")
            {
                Logger.Debug($"GetPackageType(): Package type determined as Clearing state is APPROVED");
                return PackageType.ClearedThirdParty;
            }
            else if (GetPropertyValue(Dataconstant.Cdx_IsInternal) == "TRUE")
            {
                Logger.Debug($"GetPackageType(): Package type determined as Internal");
                return PackageType.Internal;
            }
            else if (GetPropertyValue(Dataconstant.Cdx_IsDevelopment) == "TRUE")
            {
                Logger.Debug($"GetPackageType(): Package type determined as Development");
                return PackageType.Development;
            }
            Logger.Debug($"GetPackageType(): Package type determined as Unknown");
            return PackageType.Unknown;
        }

        #endregion
    }
}
