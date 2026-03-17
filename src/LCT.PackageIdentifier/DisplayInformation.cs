using LCT.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public static class DisplayInformation
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Returns a comma separated list of include file patterns for the configured project type.
        /// </summary>
        /// <param name="appSettings">Application settings containing project-type specific include lists.</param>
        /// <returns>Comma separated include file patterns or an empty string.</returns>
        public static string DisplayIncludeFiles(CommonAppSettings appSettings)
        {
            Logger.Debug("DisplayIncludeFiles():Starting to getting include files list based on project type.");
            string totalString = string.Empty;
            var includeMappings = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", () => appSettings?.Npm?.Include },
        { "NUGET", () => appSettings?.Nuget?.Include },
        { "MAVEN", () => appSettings?.Maven?.Include },
        { "DEBIAN", () => appSettings?.Debian?.Include },
        { "POETRY", () => appSettings?.Poetry?.Include },
        { "CONAN", () => appSettings?.Conan?.Include },
        { "ALPINE", () => appSettings?.Alpine?.Include },
        { "CARGO", () => appSettings?.Cargo?.Include },
        { "CHOCO", () => appSettings?.Choco?.Include }
    };

            if (includeMappings.TryGetValue(appSettings.ProjectType, out var getIncludeList))
            {
                var includeList = getIncludeList();
                if (includeList != null)
                {
                    totalString = string.Join(",", includeList);
                }
                Logger.DebugFormat("DisplayIncludeFiles():Include files for project type {0}: {1}", appSettings.ProjectType, totalString);
            }
            else
            {
                LogHandlingHelper.BasicErrorHandling("Identified invalid projecttype", "DisplayIncludeFiles()", $"Unable to retrieve exclude files because an invalid project type was provided: {appSettings.ProjectType}", "Provide Valid project type in configuration.");
                Logger.ErrorFormat("Invalid ProjectType - {0}", appSettings.ProjectType);
            }
            Logger.Debug("DisplayIncludeFiles():Completed getting include files list.\n");
            return totalString;
        }

        /// <summary>
        /// Returns a comma separated list of exclude file patterns for the configured project type.
        /// </summary>
        /// <param name="appSettings">Application settings containing project-type specific exclude lists.</param>
        /// <returns>Comma separated exclude file patterns or an empty string.</returns>
        public static string DisplayExcludeFiles(CommonAppSettings appSettings)
        {
            Logger.Debug("DisplayExcludeFiles():Starting to getting exclude files list based on project type.");
            string totalString = string.Empty;
            var excludeMappings = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", () => appSettings?.Npm?.Exclude },
        { "NUGET", () => appSettings?.Nuget?.Exclude },
        { "MAVEN", () => appSettings?.Maven?.Exclude },
        { "DEBIAN", () => appSettings?.Debian?.Exclude },
        { "POETRY", () => appSettings?.Poetry?.Exclude },
        { "CONAN", () => appSettings?.Conan?.Exclude },
        { "ALPINE", () => appSettings?.Alpine?.Exclude },
        { "CARGO", () => appSettings?.Cargo?.Exclude },
        { "CHOCO", () => appSettings?.Choco?.Exclude }
    };

            if (excludeMappings.TryGetValue(appSettings.ProjectType, out var getExcludeList))
            {
                var excludeList = getExcludeList();
                if (excludeList != null)
                {
                    totalString = string.Join(",", excludeList);
                }
                Logger.DebugFormat("DisplayExcludeFiles():Exclude files for project type {0}: {1}", appSettings.ProjectType, totalString);
            }
            else
            {
                LogHandlingHelper.BasicErrorHandling("Identified invalid projecttype", "DisplayExcludeFiles()", $"Unable to retrieve exclude files because an invalid project type was provided: {appSettings.ProjectType}", "Provide Valid project type in configuration.");
                Logger.ErrorFormat("Invalid ProjectType - {0}", appSettings.ProjectType);
            }
            Logger.Debug("DisplayExcludeFiles():Completed getting exclude files list.\n");
            return totalString;
        }

        /// <summary>
        /// Returns a comma separated list of components excluded via SW360 configuration.
        /// </summary>
        /// <param name="appSettings">Application settings that may contain SW360 exclusion configuration.</param>
        /// <returns>Comma separated excluded components or an empty string.</returns>
        public static string DisplayExcludeComponents(CommonAppSettings appSettings)
        {
            Logger.Debug("DisplayExcludeComponents():Starting to retrieve the list of excluded components.");
            string totalString = string.Empty;
            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                totalString = string.Join(",", appSettings.SW360?.ExcludeComponents?.ToList());
            }
            Logger.Debug("DisplayExcludeComponents():Completed retrieving the list of excluded components.\n");
            return totalString;
        }

        /// <summary>
        /// Returns a comma separated list of internal Artifactory repositories for the configured project type.
        /// </summary>
        /// <param name="appSettings">Application settings containing Artifactory repository lists.</param>
        /// <returns>Comma separated internal repository names or an empty string.</returns>
        public static string GetInternalRepolist(CommonAppSettings appSettings)
        {
            Logger.Debug("GetInternalRepolist():Starting to retrieve the internal repository list based on project type.");
            string listOfInternalRepoList = string.Empty;

            var repoMapping = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
        {
        { "NPM", () => appSettings?.Npm?.Artifactory.InternalRepos },
        { "NUGET", () => appSettings?.Nuget?.Artifactory.InternalRepos },
        { "MAVEN", () => appSettings?.Maven?.Artifactory.InternalRepos },
        { "DEBIAN", () => appSettings?.Debian?.Artifactory.InternalRepos },
        { "POETRY", () => appSettings?.Poetry?.Artifactory.InternalRepos },
        { "CONAN", () => appSettings?.Conan?.Artifactory.InternalRepos },
        { "ALPINE", () => appSettings?.Alpine?.Artifactory.InternalRepos },
        { "CARGO", () => appSettings?.Cargo?.Artifactory.InternalRepos },
        { "CHOCO", () => appSettings?.Choco?.Artifactory.InternalRepos }
        };

            if (repoMapping.TryGetValue(appSettings.ProjectType, out var getRepos))
            {
                var repos = getRepos();
                if (repos != null)
                {
                    listOfInternalRepoList = string.Join(",", repos);
                    Logger.DebugFormat("GetInternalRepolist():Internal repositories for project type {0}: {1}", appSettings.ProjectType, listOfInternalRepoList);
                }
            }
            else
            {
                LogHandlingHelper.BasicErrorHandling("Identified invalid projecttype", "GetInternalRepolist()", $"Unable to retrieve exclude files because an invalid project type was provided: {appSettings.ProjectType}", "Provide Valid project type in configuration.");
                Logger.ErrorFormat("Invalid ProjectType - {0}", appSettings.ProjectType);
            }
            Logger.Debug("GetInternalRepolist():Completed retrieving the internal repository list.\n");
            return listOfInternalRepoList;
        }

        /// <summary>
        /// Logs warnings to indicate missing SW360 or JFrog configuration used during BOM generation.
        /// </summary>
        /// <param name="appSettings">Application settings to inspect for SW360 and JFrog configuration.</param>
        /// <returns>void.</returns>
        public static void LogBomGenerationWarnings(CommonAppSettings appSettings)
        {
            if (appSettings.SW360 == null && appSettings.Jfrog == null)
            {
                Logger.Warn($"CycloneDX BoM file generated without using SW360 and JFrog details.");
            }
            else if (appSettings.SW360 == null)
            {
                Logger.Warn($"CycloneDX BoM file generated without using SW360 details.");
            }
            else if (appSettings.Jfrog == null)
            {
                Logger.Warn($"CycloneDX BoM file generated without using JFrog details.");
            }
        }
        #endregion

        #region Events
        #endregion
    }
}
