// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// The Alpine Processor class
    /// </summary>
    public class AlpineProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : IParser
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        #endregion

        #region Properties
        #endregion

        #region Constructors
        // Primary constructor parameters are declared on the class.
        #endregion

        #region Methods
        /// <summary>
        /// Parses Alpine package files from the configured input folder and builds a BOM.
        /// </summary>
        /// <param name="appSettings">Application settings with input and processing options.</param>
        /// <param name="unSupportedBomList">Reference BOM that will be populated with unsupported components.</param>
        /// <returns>BOM built from discovered Alpine packages.</returns>
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            List<string> configFiles;
            List<AlpinePackage> listofComponents = new List<AlpinePackage>();
            Bom bom = new Bom();
            List<Dependency> dependenciesForBOM = new();

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Alpine);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                }
                else
                {
                    Logger.Debug($"ParsePackageFile():FileName: " + filepath);
                    listofComponents.AddRange(ParseCycloneDX(filepath, dependenciesForBOM, appSettings));
                }

            }

            int initialCount = listofComponents.Count;
            int totalUnsupportedComponents = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            GetDistinctComponentList(ref listofComponents);
            List<Component> listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponents - ListUnsupportedComponentsForBom.Components.Count;
            bom.Components = listComponentForBOM;
            bom.Dependencies = dependenciesForBOM;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);

            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }

        /// <summary>
        /// Removes components excluded in settings from the provided BOM.
        /// </summary>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="cycloneDXBOM">BOM to filter.</param>
        /// <returns>Filtered BOM.</returns>
        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM,
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        /// <summary>
        /// Asynchronously retrieves repository details for the provided components and returns a modified list.
        /// </summary>
        /// <param name="componentsForBOM">List of components to enrich.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="jFrogService">JFrog service to query (may be null).</param>
        /// <param name="bomhelper">BOM helper utilities.</param>
        /// <returns>Asynchronously returns the modified list of components.</returns>
        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                CycloneBomProcessor.SetProperties(appSettings, component, ref modifiedBOM);
            }
            await Task.Yield();
            return modifiedBOM;
        }

        /// <summary>
        /// Asynchronously identifies internal components using JFrog and BOM helper; returns input unchanged in current implementation.
        /// </summary>
        /// <param name="componentData">Component identification data to analyze.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="jFrogService">JFrog service to query.</param>
        /// <param name="bomhelper">BOM helper utilities.</param>
        /// <returns>Asynchronously returns the (possibly modified) component identification data.</returns>
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            await Task.Yield();
            return componentData;
        }

        /// <summary>
        /// Parses a CycloneDX SBOM file and returns a list of AlpinePackage entries.
        /// </summary>
        /// <param name="filePath">Path to the CycloneDX/ SPDX file.</param>
        /// <param name="dependenciesForBOM">List to collect BOM dependencies.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <returns>List of parsed AlpinePackage instances.</returns>
        public List<AlpinePackage> ParseCycloneDX(string filePath, List<Dependency> dependenciesForBOM, CommonAppSettings appSettings)
        {
            List<AlpinePackage> alpinePackages = new List<AlpinePackage>();
            ExtractDetailsForJson(filePath, ref alpinePackages, dependenciesForBOM, appSettings);
            return alpinePackages;
        }

        /// <summary>
        /// Extracts package details from a BOM file and populates provided collections with packages and dependencies.
        /// </summary>
        /// <param name="filePath">Path to the BOM file.</param>
        /// <param name="alpinePackages">Reference list to append discovered Alpine packages.</param>
        /// <param name="dependenciesForBOM">List to collect dependencies discovered in the BOM.</param>
        /// <param name="appSettings">Application settings.</param>
        private void ExtractDetailsForJson(string filePath, ref List<AlpinePackage> alpinePackages, List<Dependency> dependenciesForBOM, CommonAppSettings appSettings)
        {
            Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            Bom bom = BomHelper.ParseBomFile(filePath, _spdxBomParser, _cycloneDXBomParser, appSettings, ref listUnsupportedComponents);
            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                AlpinePackage package = new AlpinePackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                    SpdxComponentDetails = new SpdxComponentInfo(),
                };
                SetSpdxComponentDetails(filePath, package, componentsInfo);

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["ALPINE"]))
                {

                    alpinePackages.Add(package);
                    Logger.Debug($"ExtractDetailsForJson():ValidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"ExtractDetailsForJson():InvalidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
            }
            if (bom.Dependencies != null)
            {
                dependenciesForBOM.AddRange(bom.Dependencies);
            }
            ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
            ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
        }

        /// <summary>
        /// Removes duplicate components from the provided list and updates KPI data.
        /// </summary>
        /// <param name="listofComponents">Reference to the component list to deduplicate.</param>
        private static void GetDistinctComponentList(ref List<AlpinePackage> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.PurlID }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        /// <summary>
        /// Builds a release external id (purl) for the specified name and version.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="version">Package version.</param>
        /// <returns>Release external id as a PURL string.</returns>
        private static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        /// <summary>
        /// Extracts the distro query portion from an Alpine package PURL if present.
        /// </summary>
        /// <param name="alpinePackage">Alpine package to inspect.</param>
        /// <returns>Distro query string including leading token, or empty string if not found.</returns>
        private static string GetDistro(AlpinePackage alpinePackage)
        {
            var distroIndex = alpinePackage.PurlID.LastIndexOf("distro");
            if (distroIndex == -1)
            {
                return string.Empty;
            }
            return alpinePackage.PurlID[distroIndex..];
        }

        /// <summary>
        /// Forms a list of CycloneDX Component objects from parsed Alpine packages, adding PURLs and properties.
        /// </summary>
        /// <param name="listOfComponents">List of parsed Alpine packages.</param>
        /// <returns>List of components ready for BOM insertion.</returns>
        private static List<Component> FormComponentReleaseExternalID(List<AlpinePackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                var distro = GetDistro(prop);
                Component component = new Component
                {
                    Name = prop.Name,
                    Version = prop.Version
                };
                component.Purl = GetReleaseExternalId(prop.Name, prop.Version);
                component.BomRef = string.IsNullOrEmpty(distro) ? $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{prop.Name}@{prop.Version}" : $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{prop.Name}@{prop.Version}?{distro}";
                component.Type = Component.Classification.Library;
                AddComponentProperties(prop, component);
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        /// <summary>
        /// Adds properties to a CycloneDX component based on SPDX information or discovery metadata.
        /// </summary>
        /// <param name="prop">Parsed Alpine package information.</param>
        /// <param name="component">Component to enrich.</param>
        private static void AddComponentProperties(AlpinePackage prop, Component component)
        {
            if (prop.SpdxComponentDetails.SpdxComponent)
            {
                string fileName = Path.GetFileName(prop.SpdxComponentDetails.SpdxFilePath);
                SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
                SpdxSbomHelper.AddDevelopmentPropertyForSpdx(prop.SpdxComponentDetails.DevComponent, component);
            }
            else
            {
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                component.Properties ??= new List<Property>();
                component.Properties.Add(identifierType);
            }
        }

        /// <summary>
        /// Sets SPDX related details on an AlpinePackage when the source file is an SPDX file.
        /// </summary>
        /// <param name="filePath">Source file path.</param>
        /// <param name="package">Alpine package to update.</param>
        /// <param name="componentInfo">Component information parsed from the BOM.</param>
        private static void SetSpdxComponentDetails(string filePath, AlpinePackage package, Component componentInfo)
        {
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                package.SpdxComponentDetails.SpdxFilePath = filePath;
                package.SpdxComponentDetails.SpdxComponent = true;
                package.SpdxComponentDetails.DevComponent = componentInfo.Properties?.Any(x => x.Name == Dataconstant.Cdx_IsDevelopment && x.Value == "true") ?? false;
            }
        }

        #endregion

        #region Events
        #endregion
    }
}
