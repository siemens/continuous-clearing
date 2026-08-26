// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using log4net;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SIT.Scan
{
    public static class CommonIdentiferHelper
    {
        #region Fields
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Returns the repository name for a prioritized order (release, devdep, dev) from AQL results.
        /// </summary>
        /// <param name="aqlResults">List of AQL results to inspect.</param>
        /// <returns>Repository name matching the preferred order or a sentinel when not found.</returns>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string GetRepodetailsFromPerticularOrder(List<AqlResult> aqlResults, Component component)
        {
            Logger.Debug("GetRepodetailsFromPerticularOrder(): Starting repository details retrieval from AQL results.");

            if (aqlResults == null)
            {
                Logger.Debug("GetRepodetailsFromPerticularOrder(): No repositories identified from aqlresult. Returning 'Not Found in Repo'.");
                return NotFoundInRepo;
            }

            Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): Total repositories identified from AQL result: {0}", aqlResults.Count);
            var repoKeywords = FindRepositoryOrder(component);
            string repo = FindRepositoryByKeywords(aqlResults, repoKeywords);

            if (repo != null)
            {
                Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): Found repository: {0}", repo);
                return repo;
            }
            repo = aqlResults.FirstOrDefault()?.Repo ?? NotFoundInRepo;
            Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): No specific repository found. Returning repository or 'Not Found in Repo': {0}", repo);
            return repo;
        }
        private static string[] FindRepositoryOrder(Component component)
        {
            bool isDevelopment = component?.Properties?.Any(p => p.Name == Dataconstant.Cdx_IsDevelopment && p.Value == "true") == true;
            bool isInternal = component?.Properties?.Any(p => p.Name == Dataconstant.Cdx_IsInternal && p.Value == "true") == true;
            if (isDevelopment&&!isInternal)
            {
                return ["devdep", "release", "dev"];
            }
            return ["release", "devdep", "dev"];
        }
        private static string FindRepositoryByKeywords(List<AqlResult> aqlResults, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                var repo = aqlResults.Find(x => x.Repo.Contains(keyword))?.Repo;
                if (repo != null)
                {
                    return repo;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the default project name to use in file naming based on SW360 configuration or a fallback.
        /// </summary>
        /// <param name="appSettings">Application settings which may contain SW360 project info.</param>
        /// <returns>Project name string for use as default.</returns>

        public static string GetDefaultProjectName(CommonAppSettings appSettings)
        {
            string projectName;
            if (appSettings.SW360 != null)
            {
                projectName = appSettings.SW360.ProjectName;
            }
            else
            {
                projectName = FileConstant.basicSBOMName;
            }

            return projectName;
        }

        /// <summary>
        /// Returns true when the component has the Cdx_IsInternal property set to "true".
        /// </summary>
        public static bool IsComponentInternal(Component component)
        {
            return component?.Properties?.Any(p => p.Name == Dataconstant.Cdx_IsInternal && p.Value == "true") == true;
        }

        /// <summary>
        /// Returns the configured internal Artifactory repositories for the current project type.
        /// </summary>
        public static IReadOnlyCollection<string> GetInternalRepos(CommonAppSettings appSettings)
        {
            if (appSettings == null || string.IsNullOrEmpty(appSettings.ProjectType))
            {
                return System.Array.Empty<string>();
            }

            var repoMapping = new Dictionary<string, System.Func<IEnumerable<string>>>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "NPM", () => appSettings.Npm?.Artifactory?.InternalRepos },
                { "NUGET", () => appSettings.Nuget?.Artifactory?.InternalRepos },
                { "MAVEN", () => appSettings.Maven?.Artifactory?.InternalRepos },
                { "DEBIAN", () => appSettings.Debian?.Artifactory?.InternalRepos },
                { "POETRY", () => appSettings.Poetry?.Artifactory?.InternalRepos },
                { "CONAN", () => appSettings.Conan?.Artifactory?.InternalRepos },
                { "ALPINE", () => appSettings.Alpine?.Artifactory?.InternalRepos },
                { "CARGO", () => appSettings.Cargo?.Artifactory?.InternalRepos },
                { "CHOCO", () => appSettings.Choco?.Artifactory?.InternalRepos }
            };

            if (repoMapping.TryGetValue(appSettings.ProjectType, out var getRepos))
            {
                return getRepos()?.Where(r => !string.IsNullOrEmpty(r)).ToList() ?? (IReadOnlyCollection<string>)System.Array.Empty<string>();
            }
            return System.Array.Empty<string>();
        }

        /// <summary>
        /// Filters the AQL result list to only include entries whose Repo matches one of the supplied internal repositories.
        /// </summary>
        public static List<AqlResult> FilterAqlResultsByRepos(List<AqlResult> aqlResultList, IEnumerable<string> repos)
        {
            if (aqlResultList == null || repos == null)
            {
                return new List<AqlResult>();
            }
            var repoSet = new HashSet<string>(repos, System.StringComparer.OrdinalIgnoreCase);
            return aqlResultList.Where(x => x.Repo != null && repoSet.Contains(x.Repo)).ToList();
        }

        /// <summary>
        /// Returns the AQL result list to use for the component based on its internal flag.
        /// Internal components are matched against the internal repo subset only.
        /// </summary>
        public static List<AqlResult> GetAqlResultsForComponent(Component component, List<AqlResult> aqlResultList, List<AqlResult> internalAqlResultList)
        {
            return IsComponentInternal(component) ? internalAqlResultList : aqlResultList;
        }

        public static Bom GetCdxGenBomData(List<string> configFiles, CommonAppSettings appSettings, System.Func<string, Bom> parseCycloneDxBom)
        {
            var cdxGenBomData = CommonHelper.GetCdxGenBomData(configFiles, parseCycloneDxBom);
            if (cdxGenBomData?.Components != null)
            {
                cdxGenBomData.Components = [.. cdxGenBomData.Components.Where(c => c.Type != Component.Classification.Application)];
                CycloneDXBomParser.CheckValidComponentsForProjectType(cdxGenBomData.Components, appSettings.ProjectType);
                if (cdxGenBomData.Dependencies != null)
                {
                    CycloneDXBomParser.CheckValidDependenciesForProjectType(cdxGenBomData.Dependencies, appSettings.ProjectType);
                }
                return cdxGenBomData;
            }
            return null;
        }


        #endregion

    }
}
