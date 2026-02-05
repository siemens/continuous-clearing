// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Common.Constants;
using LCT.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    public static class JfrogRepoUpdater
    {
        #region Fields

        private readonly static List<AqlResult> aqlResultList = new();

        #endregion

        #region Properties

        public static IJFrogService JFrogService { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously updates JFrog repository path for successfully uploaded items.
        /// </summary>
        /// <param name="m_ComponentsInBOM">The BOM containing components.</param>
        /// <param name="displayPackagesInfo">The display information for packages.</param>
        /// <returns>A task containing the updated BOM.</returns>
        public static async Task<Bom> UpdateJfrogRepoPathForSucessfullyUploadedItems(Bom m_ComponentsInBOM,
                                                                            DisplayPackagesInfo displayPackagesInfo)
        {
            // Get details of sucessfully uploaded packages
            List<ComponentsToArtifactory> uploadedPackages = PackageUploadInformation.GetUploadePackageDetails(displayPackagesInfo);

            // Get the details of all the dest repo names from jfrog at once
            List<string> destRepoNames = uploadedPackages?.Select(x => x.DestRepoName).Distinct().ToList() ?? new List<string>();
            List<AqlResult> jfrogPackagesListAql = await GetJfrogRepoInfoForAllTypePackages(destRepoNames);

            // Update the repo path
            List<Component> bomComponents = UpdateJfroRepoPathProperty(m_ComponentsInBOM, uploadedPackages, jfrogPackagesListAql);
            m_ComponentsInBOM.Components = bomComponents;
            return m_ComponentsInBOM;

        }

        /// <summary>
        /// Updates the JFrog repository path property for BOM components.
        /// </summary>
        /// <param name="m_ComponentsInBOM">The BOM containing components.</param>
        /// <param name="uploadedPackages">The list of uploaded packages.</param>
        /// <param name="jfrogPackagesListAql">The list of JFrog packages from AQL query.</param>
        /// <returns>A list of updated components.</returns>
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
                string packageNameEXtension = PackageUploadHelper.GetPackageNameExtensionBasedOnComponentType(package);
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

        /// <summary>
        /// Gets the JFrog repository path from AQL result.
        /// </summary>
        /// <param name="aqlResult">The AQL result containing repository information.</param>
        /// <returns>The formatted JFrog repository path.</returns>
        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }
            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        /// <summary>
        /// Gets the JFrog information of the uploaded package from AQL results.
        /// </summary>
        /// <param name="jfrogPackagesListAql">The list of JFrog packages from AQL query.</param>
        /// <param name="package">The package to find information for.</param>
        /// <param name="packageNameEXtension">The package name extension.</param>
        /// <returns>The AQL result for the uploaded package, or null if not found.</returns>
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

        /// <summary>
        /// Asynchronously gets JFrog repository information for all package types.
        /// </summary>
        /// <param name="destRepoNames">The list of destination repository names.</param>
        /// <returns>A task containing a list of AQL results for all packages.</returns>
        public static async Task<List<AqlResult>> GetJfrogRepoInfoForAllTypePackages(List<string> destRepoNames)
        {
            if (destRepoNames != null && destRepoNames.Count > 0)
            {
                foreach (var repo in destRepoNames)
                {
                    var result = await JFrogService.GetInternalComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(result);
                }
            }

            return aqlResultList;
        }

        #endregion
    }
}
