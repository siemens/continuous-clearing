using LCT.APICommunications.Model.AQL;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using CycloneDX.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using LCT.Common.Constants;
using LCT.Services;
using System;
using LCT.Services.Interface;
using LCT.APICommunications;

namespace LCT.ArtifactoryUploader
{
    public class UploadToArtifactory
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IJFrogService jFrogService { get; set; }
        private static List<AqlResult> aqlResultList = new();
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
                    await AddUnknownPackagesAsync(item, displayPackagesInfo);
                }
            }
            Logger.Debug("Ending GetComponentsToBeUploadedToArtifactory() method");
            return componentsToBeUploaded;
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
                return "POETRY";
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

        private static string GetDestinationRepo(Component item, CommonAppSettings appSettings)
        {
            var packageType = GetPackageType(item);
            var componentType = GetComponentType(item);

            if (!string.IsNullOrEmpty(componentType))
            {
                switch (componentType.ToLower())
                {
                    case "npm":
                        return GetRepoName(packageType, appSettings.Npm.ReleaseRepo, appSettings.Npm.DevDepRepo, appSettings.Npm.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                    case "nuget":
                        return GetRepoName(packageType, appSettings.Nuget.ReleaseRepo, appSettings.Nuget.DevDepRepo, appSettings.Nuget.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                    case "maven":
                        return GetRepoName(packageType, appSettings.Maven.ReleaseRepo, appSettings.Maven.DevDepRepo, appSettings.Maven.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                    case "poetry":
                        return GetRepoName(packageType, appSettings.Poetry.ReleaseRepo, appSettings.Poetry.DevDepRepo, appSettings.Poetry.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                    case "conan":
                        return GetRepoName(packageType, appSettings.Conan.ReleaseRepo, appSettings.Conan.DevDepRepo, appSettings.Conan.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                    case "debian":
                        return GetRepoName(packageType, appSettings.Debian.ReleaseRepo, appSettings.Debian.DevDepRepo, appSettings.Debian.Artifactory.ThirdPartyRepos.Where(x => x.Upload.Equals(true)).FirstOrDefault()?.Name);
                }
            }

            return string.Empty;
        }

        private static string GetPackagePath(ComponentsToArtifactory component, AqlResult aqlResult)
        {
            switch (component.ComponentType)
            {
                case "NPM":
                    if (aqlResult != null)
                    {
                        return $"{aqlResult.Path}";
                    }
                    else
                    {
                        return $"{component.Name}/-";
                    }

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
        public static string GetCopyURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == "NPM")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}";

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
            else if (component.ComponentType == "POETRY")
            {
                url = $"{component.JfrogApi}{ApiConstant.CopyPackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
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

        public static string GetMoveURL(ComponentsToArtifactory component)
        {
            string url = string.Empty;
            if (component.ComponentType == "NPM")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
              $"?to=/{component.DestRepoName}/{component.Path}/{component.PypiOrNpmCompName}";

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
            else if (component.ComponentType == "POETRY")
            {
                url = $"{component.JfrogApi}{ApiConstant.MovePackageApi}{component.SrcRepoPathWithFullName}" +
               $"?to=/{component.DestRepoName}/{component.PypiOrNpmCompName}";
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

        private static string GetJfrogPackageName(ComponentsToArtifactory component)
        {
            return component.ComponentType switch
            {
                "NPM" => component.PypiOrNpmCompName,
                "NUGET" => $"{component.PackageName}.{component.Version}{ApiConstant.NugetExtension}",
                "DEBIAN" => $"{component.PackageName}_{component.Version.Replace(ApiConstant.DebianExtension, "") + "*"}",
                "POETRY" => component.PypiOrNpmCompName,
                _ => string.Empty,
            };
        }

        private static async Task AddUnknownPackagesAsync(Component item, DisplayPackagesInfo displayPackagesInfo)
        {
            string GetPropertyValue(string propertyName) =>
                  item.Properties
                      .Find(p => p.Name == propertyName)?
                      .Value?
                      .ToUpperInvariant();

            if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "NPM")
            {

                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesNpm.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "NUGET")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesNuget.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "MAVEN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesMaven.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "POETRY")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesPython.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "CONAN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesConan.Add(components);
            }
            else if (GetPropertyValue(Dataconstant.Cdx_ProjectType) == "DEBIAN")
            {
                ComponentsToArtifactory components = await GetUnknownPackageinfo(item);
                displayPackagesInfo.UnknownPackagesDebian.Add(components);
            }

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

        private static AqlResult GetArtifactoryRepoNameForConan(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";

            AqlResult repoName = aqlResultList.Find(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            return repoName;
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

    }
}
