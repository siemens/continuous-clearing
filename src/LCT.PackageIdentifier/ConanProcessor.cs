// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;


namespace LCT.PackageIdentifier
{

    /// <summary>
    /// Parses the Conan Packages
    /// </summary>
    public class ConanProcessor : CycloneDXBomParser, IParser
    {
        #region fields
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly CycloneDXBomParser cycloneDXBomParser;
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        #endregion

        #region constructor
        public ConanProcessor()
        {
            if (cycloneDXBomParser == null)
            {
                cycloneDXBomParser = new CycloneDXBomParser();
            }
        }
        #endregion

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new List<Component>();
            Bom bom = new Bom();
            int totalComponentsIdentified = 0;

            ParsingInputFileForBOM(appSettings, ref componentsForBOM);
            totalComponentsIdentified = componentsForBOM.Count;

            componentsForBOM = GetExcludedComponentsList(componentsForBOM);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();

            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;
            var componentsWithMultipleVersions = componentsForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            if (componentsWithMultipleVersions.Count != 0)
            {
                Logger.Warn($"Multiple versions detected :\n");
                foreach (var item in componentsWithMultipleVersions)
                {
                    Logger.Warn($"Component Name : {item.Name}\nComponent Version : {item.Version}\nPackage Found in : {item.Description}\n");
                }
            }
            bom.Components = componentsForBOM;
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repository
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.InternalRepoList, jFrogService);

            // find the components in the list of internal components
            List<Component> internalComponents = new List<Component>();
            var internalComponentStatusUpdatedList = new List<Component>();
            var inputIterationList = componentData.comparisonBOMData;

            foreach (Component component in inputIterationList)
            {
                var currentIterationItem = component;
                bool isTrue = IsInternalConanComponent(aqlResultList, currentIterationItem);
                if (currentIterationItem.Properties?.Count == null || currentIterationItem.Properties?.Count <= 0)
                {
                    currentIterationItem.Properties = new List<Property>();
                }

                Property isInternal = new() { Name = Dataconstant.Cdx_IsInternal, Value = "false" };
                if (isTrue)
                {
                    internalComponents.Add(currentIterationItem);
                    isInternal.Value = "true";
                }
                else
                {
                    isInternal.Value = "false";
                }

                currentIterationItem.Properties.Add(isInternal);
                internalComponentStatusUpdatedList.Add(currentIterationItem);
            }

            // update the comparison BOM data
            componentData.comparisonBOMData = internalComponentStatusUpdatedList;
            componentData.internalComponents = internalComponents;

            return componentData;
        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repository
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Conan?.JfrogConanRepoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                string repoName = GetArtifactoryRepoName(aqlResultList, component);
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoUrl, Value = repoName };
                Component componentVal = component;

                if (componentVal.Properties?.Count == null || componentVal.Properties?.Count <= 0)
                {
                    componentVal.Properties = new List<Property>();
                }
                componentVal.Properties.Add(artifactoryrepo);
                componentVal.Properties.Add(projectType);
                componentVal.Description = string.Empty;

                modifiedBOM.Add(componentVal);
            }

            return modifiedBOM;
        }

        public static bool IsDevDependency(ConanPackage component, ConanPackage rootNode, ref int noOfDevDependent)
        {
            var isDev = false;
            if (rootNode.DevDependencies != null && rootNode.DevDependencies.Contains(component.Id))
            {
                isDev = true;
                noOfDevDependent++;
            }

            return isDev;
        }

        #endregion

        #region private methods
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> componentsForBOM)
        {
            List<string> configFiles;
            configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Conan);

            foreach (string filepath in configFiles)
            {
                Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);
                var components = ParsePackageLockJson(filepath, appSettings);
                AddingIdentifierType(components, "PackageFile");
                componentsForBOM.AddRange(components);
            }
        }

        private List<Component> ParsePackageLockJson(string filepath, CommonAppSettings appSettings)
        {
            List<Component> lstComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;
            int noOfExcludedComponents = 0;
            try
            {
                string jsonContent = File.ReadAllText(filepath);

                var jsonDeserialized = JObject.Parse(jsonContent);
                var nodes = jsonDeserialized["graph_lock"]["nodes"];

                List<ConanPackage> nodePackages = new List<ConanPackage>();
                foreach (var node in nodes)
                {
                    string nodeId = ((JProperty)node).Name;
                    var conanPackage = JsonConvert.DeserializeObject<ConanPackage>(((JProperty)node).Value.ToString());
                    conanPackage.Id = nodeId;
                    nodePackages.Add(conanPackage);
                }

                GetPackagesForBom(ref lstComponentForBOM, ref noOfDevDependent, nodePackages);

                if (appSettings.Conan.ExcludedComponents != null)
                {
                    lstComponentForBOM = CommonHelper.RemoveExcludedComponents(lstComponentForBOM, appSettings.Conan.ExcludedComponents, ref noOfExcludedComponents);
                    BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

                }

                BomCreator.bomKpiData.DevDependentComponents += noOfDevDependent;
            }
            catch (JsonReaderException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"ParsePackageFile():", ex);
            }
            catch (IOException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"ParsePackageFile():", ex);
            }
            catch (SecurityException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"ParsePackageFile():", ex);
            }

            return lstComponentForBOM;
        }

        private static void GetPackagesForBom(ref List<Component> lstComponentForBOM, ref int noOfDevDependent, List<ConanPackage> nodePackages)
        {
            var rootNode = nodePackages.FirstOrDefault();
            if (!rootNode.Dependencies.Any() || rootNode.Dependencies == null)
            {
                throw new ArgumentNullException(nameof(nodePackages), "Dependency(requires) node name details not present in the root node.");
            }
            
            // Ignoring the root node as it is the package information node.
            foreach (var component in nodePackages.Skip(1))
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += 1;
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                if (string.IsNullOrEmpty(component.Reference))
                {
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile--;
                    continue;
                }

                Component components = new Component();

                // dev components are not ignored and added as a part of SBOM   
                if (IsDevDependency(component, rootNode, ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                string packageName = Convert.ToString(component.Reference);

                if (packageName.Contains('/'))
                {
                    components.Name = packageName.Split(new char[] { '/', '@' })[0];
                    components.Version = packageName.Split(new char[] { '/', '@' })[1];
                }
                else
                {
                    components.Name = packageName;
                }

                components.Purl = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";
                components.BomRef = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";

                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                lstComponentForBOM.Add(components);
            }
        }

        private static bool IsInternalConanComponent(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            if (aqlResultList.Exists(
                x => x.Path.Contains(jfrogcomponentPath, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";

            string repoName = aqlResultList.Find(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;

            return repoName;
        }

        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["CONAN"]))
                {
                    components.Add(componentsInfo);
                    Logger.Debug($"GetExcludedComponentsList():ValidComponent For CONAN : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For CONAN : Component Details : {componentsInfo?.Name} @ {componentsInfo?.Version} @ {componentsInfo?.Purl}");
                }
            }
            return components;
        }

        private static void AddingIdentifierType(List<Component> components, string identifiedBy)
        {
            foreach (var component in components)
            {
                if (component.Properties == null)
                {
                    component.Properties = new List<Property>();
                }

                Property isDev;
                Property identifierType;
                if (identifiedBy == "PackageFile")
                {
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                    component.Properties.Add(identifierType);
                }
                else
                {
                    isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                    component.Properties.Add(isDev);
                    component.Properties.Add(identifierType);
                }
            }
        }

        #endregion
    }
}
