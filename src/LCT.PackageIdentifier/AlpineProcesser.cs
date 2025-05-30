﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// The AlpineProcessor class
    /// </summary>
    public class AlpineProcessor : IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser;
        public AlpineProcessor(ICycloneDXBomParser cycloneDXBomParser)
        {
            _cycloneDXBomParser = cycloneDXBomParser;
        }

        #region public method

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles;
            List<AlpinePackage> listofComponents = new List<AlpinePackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;
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
                    listofComponents.AddRange(ParseCycloneDX(filepath, dependenciesForBOM));
                }

            }

            int initialCount = listofComponents.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;

            bom.Components = listComponentForBOM;
            bom.Dependencies = dependenciesForBOM;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);

            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            return bom;
        }

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            List<Dependency> dependenciesForBOM = cycloneDXBOM.Dependencies?.ToList() ?? new List<Dependency>();
            int noOfExcludedComponents = 0;
            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings?.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                dependenciesForBOM = CommonHelper.RemoveInvalidDependenciesAndReferences(componentForBOM, dependenciesForBOM);
                BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;

            }
            cycloneDXBOM.Components = componentForBOM;
            cycloneDXBOM.Dependencies = dependenciesForBOM;
            return cycloneDXBOM;
        }

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

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            await Task.Yield();
            return componentData;
        }

        #endregion

        #region private methods

        public List<AlpinePackage> ParseCycloneDX(string filePath, List<Dependency> dependenciesForBOM)
        {
            List<AlpinePackage> alpinePackages = new List<AlpinePackage>();
            ExtractDetailsForJson(filePath, ref alpinePackages, dependenciesForBOM);
            return alpinePackages;
        }

        private void ExtractDetailsForJson(string filePath, ref List<AlpinePackage> alpinePackages, List<Dependency> dependenciesForBOM)
        {
            Bom bom = _cycloneDXBomParser.ParseCycloneDXBom(filePath);
            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                AlpinePackage package = new AlpinePackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                };

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
        }

        private static void GetDistinctComponentList(ref List<AlpinePackage> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.PurlID }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        private static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static string GetDistro(AlpinePackage alpinePackage)
        {
            var distro = alpinePackage.PurlID;
            distro = distro.Substring(distro.LastIndexOf("distro"));
            return distro;
        }

        private static List<Component> FormComponentReleaseExternalID(List<AlpinePackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                var distro = GetDistro(prop);
                Component component = new Component
                {
                    Name = prop.Name,
                    Version = prop.Version,
                    Purl = GetReleaseExternalId(prop.Name, prop.Version)
                };
                component.BomRef = $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{prop.Name}@{prop.Version}?{distro}";
                component.Type = Component.Classification.Library;
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                component.Properties = new List<Property> { identifierType };
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        #endregion
    }
}
