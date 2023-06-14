// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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

        #region public method

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Debian);
            List<DebianPackage> listofComponents = new List<DebianPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(".xml") || filepath.EndsWith(".json"))
                {
                    listofComponents.AddRange(ParseCycloneDX(filepath));
                }
            }

            int initialCount = listofComponents.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = listComponentForBOM.Count;
            bom.Components = listComponentForBOM;
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

        private static List<DebianPackage> ParseCycloneDX(string filePath)
        {
            List<DebianPackage> debianPackages = new List<DebianPackage>();
            try
            {
                if (filePath.EndsWith(".xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);
                    XmlNodeList PackageNodes = doc.GetElementsByTagName("components");

                    foreach (XmlNode node in PackageNodes)
                    {
                        ExtractDetailsForXML(node.ChildNodes, ref debianPackages);
                    }
                }
                else if (filePath.EndsWith(".json"))
                {
                    ExtractDetailsForJson(filePath, ref debianPackages);
                }
                else
                {
                    // do nothing
                }
            }
            catch (XmlException ex)
            {
                Logger.Debug($"ParseCycloneDX", ex);
            }
            return debianPackages;
        }

        private static void ExtractDetailsForXML(XmlNodeList packageNodes, ref List<DebianPackage> debianPackages)
        {
            foreach (XmlNode packageinfo in packageNodes)
            {
                if (packageinfo.Name == "component")
                {
                    DebianPackage package = GetPackageDetails(packageinfo);
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;

                    if (!string.IsNullOrEmpty(package.Name) && !string.IsNullOrEmpty(package.Version) && !string.IsNullOrEmpty(package.PurlID) && package.PurlID.Contains(Dataconstant.DebianPackage))
                    {
                        BomCreator.bomKpiData.DebianComponents++;
                        debianPackages.Add(package);
                        Logger.Debug($"ExtractDetailsForXML():ValidComponent:Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                    }
                    else
                    {
                        BomCreator.bomKpiData.ComponentsExcluded++;
                        Logger.Debug($"ExtractDetailsForXML():InvalidComponent:Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                    }
                }
            }
        }

        private static void ExtractDetailsForJson(string filePath, ref List<DebianPackage> debianPackages)
        {
            Model.CycloneDxBomData cycloneDxBomData;
            string json = File.ReadAllText(filePath);
            cycloneDxBomData = JsonConvert.DeserializeObject<CycloneDxBomData>(json);

            if (cycloneDxBomData != null && cycloneDxBomData.ComponentsInfo != null)
            {
                foreach (var componentsInfo in cycloneDxBomData.ComponentsInfo)
                {
                    if (componentsInfo.Type == "library")
                    {
                        BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                        DebianPackage package = new DebianPackage
                        {
                            Name = componentsInfo.Name,
                            Version = componentsInfo.Version,
                            PurlID = componentsInfo.ReleaseExternalId,
                        };

                        if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.ReleaseExternalId) && componentsInfo.ReleaseExternalId.Contains(Dataconstant.DebianPackage))
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
                }
            }
            else
            {
                Logger.Debug($"ExtractDetailsForJson():NoComponenstFound!!");
            }
        }

        private static DebianPackage GetPackageDetails(XmlNode packageinfo)
        {
            DebianPackage package = new DebianPackage();
            foreach (XmlNode mainNode in packageinfo.ChildNodes)
            {
                if (mainNode.Name == "name")
                {
                    package.Name = mainNode.InnerText;
                }
                if (mainNode.Name == "version")
                {
                    package.Version = mainNode.InnerText;
                }
                if (mainNode.Name == "purl")
                {
                    package.PurlID = mainNode.InnerText;
                }
            }
            return package;
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

            return $"{Dataconstant.DebianPackage}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
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
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        #endregion
    }
}
