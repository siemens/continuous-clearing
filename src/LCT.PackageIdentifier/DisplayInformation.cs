using LCT.Common;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public static class DisplayInformation
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string DisplayIncludeFiles(CommonAppSettings appSettings)
        {
            string totalString = string.Empty;
            var includeMappings = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", () => appSettings?.Npm?.Include },
        { "NUGET", () => appSettings?.Nuget?.Include },
        { "MAVEN", () => appSettings?.Maven?.Include },
        { "DEBIAN", () => appSettings?.Debian?.Include },
        { "POETRY", () => appSettings?.Poetry?.Include },
        { "CONAN", () => appSettings?.Conan?.Include },
        { "ALPINE", () => appSettings?.Alpine?.Include }
    };

            if (includeMappings.TryGetValue(appSettings.ProjectType, out var getIncludeList))
            {
                var includeList = getIncludeList();
                if (includeList != null)
                {
                    totalString = string.Join(",", includeList);
                }
            }
            else
            {
                Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
            }

            return totalString;
        }
        public static string DisplayExcludeFiles(CommonAppSettings appSettings)
        {
            string totalString = string.Empty;
            var excludeMappings = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", () => appSettings?.Npm?.Exclude },
        { "NUGET", () => appSettings?.Nuget?.Exclude },
        { "MAVEN", () => appSettings?.Maven?.Exclude },
        { "DEBIAN", () => appSettings?.Debian?.Exclude },
        { "POETRY", () => appSettings?.Poetry?.Exclude },
        { "CONAN", () => appSettings?.Conan?.Exclude },
        { "ALPINE", () => appSettings?.Alpine?.Exclude }
    };

            if (excludeMappings.TryGetValue(appSettings.ProjectType, out var getExcludeList))
            {
                var excludeList = getExcludeList();
                if (excludeList != null)
                {
                    totalString = string.Join(",", excludeList);
                }
            }
            else
            {
                Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
            }

            return totalString;
        }

        public static string DisplayExcludeComponents(CommonAppSettings appSettings)
        {

            string totalString = string.Empty;
            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                totalString = string.Join(",", appSettings.SW360?.ExcludeComponents?.ToList());
            }
            return totalString;
        }

        public static string GetInternalRepolist(CommonAppSettings appSettings)
        {
            string listOfInternalRepoList = string.Empty;

            var repoMapping = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
        {
        { "NPM", () => appSettings?.Npm?.Artifactory.InternalRepos },
        { "NUGET", () => appSettings?.Nuget?.Artifactory.InternalRepos },
        { "MAVEN", () => appSettings?.Maven?.Artifactory.InternalRepos },
        { "DEBIAN", () => appSettings?.Debian?.Artifactory.InternalRepos },
        { "POETRY", () => appSettings?.Poetry?.Artifactory.InternalRepos },
        { "CONAN", () => appSettings?.Conan?.Artifactory.InternalRepos },
        { "ALPINE", () => appSettings?.Alpine?.Artifactory.InternalRepos }
        };

            if (repoMapping.TryGetValue(appSettings.ProjectType, out var getRepos))
            {
                var repos = getRepos();
                if (repos != null)
                {
                    listOfInternalRepoList = string.Join(",", repos);
                }
            }
            else
            {
                Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
            }

            return listOfInternalRepoList;
        }        
        public static void LogBomGenerationWarnings(CommonAppSettings appSettings)
        {
            if (appSettings.SW360 == null && appSettings.Jfrog == null)
            {
                Logger.Warn($"CycloneDX Bom file generated without using SW360 and Jfrog details.");
            }
            else if (appSettings.SW360 == null)
            {
                Logger.Warn($"CycloneDX Bom file generated without using SW360 details.");
            }
            else if (appSettings.Jfrog == null)
            {
                Logger.Warn($"CycloneDX Bom file generated without using Jfrog details.");
            }
        }
    }
}
