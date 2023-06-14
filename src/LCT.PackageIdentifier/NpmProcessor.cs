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
    /// Parses the NPM Packages
    /// </summary>
    public class NpmProcessor : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string Bundled = "bundled";
        private const string Dependencies = "dependencies";
        private const string Dev = "dev";
        private const string Version = "version";
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new List<Component>();
            Bom bom = new Bom();

            int totalComponentsIdentified = 0;


            ParsingInputFileForBOM(appSettings, ref componentsForBOM, ref bom);

            totalComponentsIdentified = componentsForBOM.Count;

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

        public List<Component> ParsePackageLockJson(string filepath, CommonAppSettings appSettings)
        {
            List<BundledComponents> bundledComponents = new List<BundledComponents>();
            List<Component> lstComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;
            int noOfExcludedComponents = 0;
            try
            {
                string jsonContent = File.ReadAllText(filepath);
                var jsonDeserialized = JObject.Parse(jsonContent);
                var dependencies = jsonDeserialized[Dependencies];

                // multi level dependency check
                if (dependencies?.Children() != null)
                {
                    IEnumerable<JProperty> depencyComponentList = dependencies.Children().OfType<JProperty>();
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM, ref noOfDevDependent, depencyComponentList);
                }

                if (appSettings.Npm.ExcludedComponents != null)
                {
                    lstComponentForBOM = CommonHelper.RemoveExcludedComponents(lstComponentForBOM, appSettings.Npm.ExcludedComponents, ref noOfExcludedComponents);
                    BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

                }
                BomCreator.bomKpiData.DevDependentComponents += noOfDevDependent;
                BomCreator.bomKpiData.BundledComponents += bundledComponents.Count;
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

        private void GetComponentsForBom(string filepath, CommonAppSettings appSettings,
            ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM,
            ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();

            foreach (JProperty prop in depencyComponentList)
            {
                Component components = new Component();
                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // ignoring the dev= true components, because they are not needed in clearing     
                if (IsDevDependency(appSettings.RemoveDevDependency, prop.Value[Dev], ref noOfDevDependent))
                {
                    continue;
                }

                IEnumerable<JProperty> subDependencyComponentList = prop.Value[Dependencies]?.OfType<JProperty>();
                if (subDependencyComponentList != null)
                {
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM, ref noOfDevDependent, subDependencyComponentList);
                }

                GetBundledComponents(prop.Value[Dependencies], ref bundledComponents);
                string componentName = prop.Name.StartsWith('@') ? prop.Name.Replace("@", "%40") : prop.Name;

                string folderPath = CommonHelper.TrimEndOfString(filepath, $"\\{FileConstant.PackageLockFileName}");

                if (prop.Name.Contains('@'))
                {
                    components.Group = prop.Name.Split('/')[0];
                    components.Name = prop.Name.Split('/')[1];

                }
                else
                {
                    components.Name = prop.Name;
                }

                components.Description = folderPath;
                components.Version = Convert.ToString(properties[Version]);
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                lstComponentForBOM.Add(components);
                lstComponentForBOM = RemoveBundledComponentFromList(bundledComponents, lstComponentForBOM);
            }
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
            ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.InternalRepoList, jFrogService);

            // find the components in the list of internal components
            List<Component> internalComponents = new List<Component>();
            var internalComponentStatusUpdatedList = new List<Component>();
            var inputIterationList = componentData.comparisonBOMData;

            foreach (Component component in inputIterationList)
            {
                var currentIterationItem = component;
                bool isTrue = IsInternalNpmComponent(aqlResultList, currentIterationItem, bomhelper);
                if (currentIterationItem.Properties?.Count == null || currentIterationItem.Properties?.Count <= 0)
                {
                    currentIterationItem.Properties = new List<Property>();
                }

                Property isInternal = new() { Name = Dataconstant.Cdx_IsInternal, Value = "false" };
                if (isTrue)
                {
                    internalComponents.Add(currentIterationItem);
                    continue;
                }

                currentIterationItem.Properties.Add(isInternal);
                internalComponentStatusUpdatedList.Add(currentIterationItem);
            }

            // update the comparision bom data
            componentData.comparisonBOMData = internalComponentStatusUpdatedList;
            componentData.internalComponents = internalComponents;

            return componentData;
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Npm?.JfrogNpmRepoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper);
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

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            int noOfExcludedComponents = 0;
            if (appSettings.Npm.ExcludedComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings.Npm.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

            }
            cycloneDXBOM.Components = componentForBOM;
            return cycloneDXBOM;
        }

        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom)
        {
            List<string> configFiles;

            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {
                Logger.Debug($"ParsePackageFile():Start");

                configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Npm);


                foreach (string filepath in configFiles)
                {
                    componentsForBOM.AddRange(ParsePackageLockJson(filepath, appSettings));
                }
            }
            else
            {
                bom = ParseCycloneDXBom(appSettings.CycloneDxBomFilePath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = bom.Components.Count;
                bom = RemoveExcludedComponents(appSettings, bom);

                componentsForBOM = bom.Components;
            }
        }

        private static bool IsDevDependency(bool removeDevDependency, JToken devValue, ref int noOfDevDependent)
        {
            if (devValue != null)
            {
                noOfDevDependent++;
            }

            return removeDevDependency && devValue != null;
        }

        private static void GetBundledComponents(JToken subdependencies, ref List<BundledComponents> bundledComponents)
        {
            //changes for components with property "bundled:true" shouldn't be on BOMs 
            //and finally in SW360 Portal
            //checking for dependencies of each component         
            if (subdependencies != null)
            {
                foreach (JProperty sub in subdependencies.OfType<JProperty>())
                {
                    var dependentProperty = JObject.Parse(Convert.ToString(sub.Value));
                    string version = Convert.ToString(dependentProperty[Version]);

                    //check for duplicate components in the list
                    if (dependentProperty[Bundled] != null &&
                       !(bundledComponents.Any(x => x.Name == sub.Name && x.Version.ToLowerInvariant() == version)))
                    {
                        BundledComponents component = new() { Name = sub.Name, Version = version };
                        bundledComponents.Add(component);
                    }
                }
            }
        }

        private static List<Component> RemoveBundledComponentFromList(List<BundledComponents> bundledComponents, List<Component> lstComponentForBOM)
        {
            List<Component> components = new List<Component>();
            components.AddRange(lstComponentForBOM);

            foreach (var componentsToBOM in lstComponentForBOM.Where(x => bundledComponents.Any(y => y.Name == x.Name &&
                y.Version.ToLowerInvariant() == x.Version.ToLowerInvariant())))
            {
                components.Remove(componentsToBOM);
            }
            return components;
        }

        private static bool IsInternalNpmComponent(
            List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}.tgz";
            if (aqlResultList.Exists(
                x => x.Name.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}.tgz";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)
                && aqlResultList.Exists(
                    x => x.Name.Equals(fullNameVersion, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}.tgz";

            string repoName = aqlResultList.Find(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}.tgz";

            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) &&
                repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                repoName = aqlResultList.Find(x => x.Name.Equals(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;
            }

            return repoName;
        }
    }
}
