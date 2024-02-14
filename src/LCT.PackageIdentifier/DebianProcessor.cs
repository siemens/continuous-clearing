// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// The DebianProcessor class
    /// </summary>
    public class DebianProcessor : IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly CycloneDXBomParser cycloneDXBomParser;

        public DebianProcessor()
        {
            cycloneDXBomParser = new CycloneDXBomParser();
        }

        #region public method

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles;
            List<DebianPackage> listofComponents = new List<DebianPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;

            configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Debian);

            foreach (string filepath in configFiles)
            {
                if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsePackageFile():FileName: " + filepath);
                    var list = ParseCycloneDX(filepath, ref bom);
                    listofComponents.AddRange(list);
                }
            }

            int initialCount = listofComponents.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;

            bom.Components = listComponentForBOM;
            if (File.Exists(appSettings.CycloneDxSBomTemplatePath) && appSettings.CycloneDxSBomTemplatePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                Bom templateDetails;
                templateDetails = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(cycloneDXBomParser.ParseCycloneDXBom(appSettings.CycloneDxSBomTemplatePath));
                CycloneDXBomParser.CheckValidComponentsForProjectType(templateDetails.Components, appSettings.ProjectType);
                //Adding Template Component Details & MetaData
                SbomTemplate.AddComponentDetails(bom.Components, templateDetails);
            }

            bom = RemoveExcludedComponents(appSettings, bom);
            return bom;
        }

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            int noOfExcludedComponents = 0;
            if (appSettings.Debian.ExcludedComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings.Debian.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;
            }
            cycloneDXBOM.Components = componentForBOM;
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

        public List<DebianPackage> ParseCycloneDX(string filePath, ref Bom bom)
        {
            List<DebianPackage> debianPackages = new List<DebianPackage>();
            bom = ExtractDetailsForJson(filePath, ref debianPackages);
            return debianPackages;
        }

        private Bom ExtractDetailsForJson(string filePath, ref List<DebianPackage> debianPackages)
        {
            Bom bom = cycloneDXBomParser.ParseCycloneDXBom(filePath);

            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                DebianPackage package = new DebianPackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                };

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                {
                    BomCreator.bomKpiData.DebianComponents++;
                    debianPackages.Add(package);
                    Logger.Debug($"ExtractDetailsForJson():ValidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"ExtractDetailsForJson():InvalidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
            }
            return bom;
        }

        private static void GetDistinctComponentList(ref List<DebianPackage> listofComponents)
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

            return $"{Dataconstant.PurlCheck()["DEBIAN"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static List<Component> FormComponentReleaseExternalID(List<DebianPackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                Component component = new Component
                {
                    Name = prop.Name,
                    Version = prop.Version,
                    Purl = GetReleaseExternalId(prop.Name, prop.Version)
                };
                component.BomRef = component.Purl;

                //For Debian projects we will be considering CycloneDX file reading components as Discovered
                //since it's Discovered from syft Tool
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                component.Properties = new List<Property> { identifierType };

                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        #endregion
    }
}
