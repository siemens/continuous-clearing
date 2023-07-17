// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Json.Converters;
using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private const string Requires = "requires";


        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new List<Component>();
            Bom bom = new Bom();
            List<Dependency> dependencies = new List<Dependency>();
            int totalComponentsIdentified = 0;

            ParsingInputFileForBOM(appSettings, ref componentsForBOM, ref bom, ref dependencies);


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
            bom.Dependencies = dependencies;
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

                // the below logic for angular 16+version due to package-lock.json file format change
                if (dependencies == null)
                {
                    var pacakages = jsonDeserialized["packages"];
                    if (pacakages?.Children() != null)
                    {
                        IEnumerable<JProperty> depencyComponentList = pacakages?.Children().OfType<JProperty>();
                        GetPackagesForBom(filepath, ref bundledComponents, ref lstComponentForBOM,
                            ref noOfDevDependent, depencyComponentList);
                    }
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

        private static void GetPackagesForBom(string filepath, ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM, ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();
       
            foreach (JProperty prop in depencyComponentList)
            {
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                if (string.IsNullOrEmpty(prop.Name))
                {
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile--;
                    continue;
                }

                Component components = new Component();
                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM   
                if (IsDevDependency( prop.Value[Dev], ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                string folderPath = CommonHelper.TrimEndOfString(filepath, $"\\{FileConstant.PackageLockFileName}");
                string packageName = CommonHelper.GetSubstringOfLastOccurance(prop.Name, $"node_modules/");
                string componentName = packageName.StartsWith('@') ? packageName.Replace("@", "%40") : packageName;

                if (packageName.Contains('@'))
                {
                    components.Group = packageName.Split('/')[0];
                    components.Name = packageName.Split('/')[1];
                }
                else
                {
                    components.Name = packageName;
                }

                components.Description = folderPath;
                components.Version = Convert.ToString(properties[Version]);
                components.Author = prop?.Value[Dependencies]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";

                CheckAndAddToBundleComponents(bundledComponents, prop, components);
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                lstComponentForBOM.Add(components);
                lstComponentForBOM = RemoveBundledComponentFromList(bundledComponents, lstComponentForBOM);
            }
        }

        private static void CheckAndAddToBundleComponents(List<BundledComponents> bundledComponents, JProperty prop, Component components)
        {
            if (prop.Value[Bundled] != null &&
                  !(bundledComponents.Any(x => x.Name == components.Name && x.Version.ToLowerInvariant() == components.Version)))
            {
                BundledComponents component = new() { Name = components.Name, Version = components.Version };
                bundledComponents.Add(component);
            }
        }


        private void GetComponentsForBom(string filepath, CommonAppSettings appSettings,
            ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM,
            ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();

            foreach (JProperty prop in depencyComponentList)
            {
                Component components = new Component();
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM    
                if (IsDevDependency(prop.Value[Dev], ref noOfDevDependent))
                {
                    isdev.Value = "true";
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
                components.Author = prop?.Value[Requires]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
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
                    isInternal.Value = "true";
                }
                else
                {
                    isInternal.Value = "false";
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

        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            List<string> configFiles;
  
            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {
                configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Npm);
            
                foreach (string filepath in configFiles)
                {
                    Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);

                    if (filepath.EndsWith(FileConstant.CycloneDXFileExtension))
                    {
                        Logger.Debug($"ParsingInputFileForBOM():Found as CycloneDXFile");
                        bom = ParseCycloneDXBom(filepath);
                        bom = RemoveExcludedComponents(appSettings, bom);

                        componentsForBOM.AddRange(bom.Components);
                    }
                    else
                    {
                        Logger.Debug($"ParsingInputFileForBOM():Found as Package File");
                        var components = ParsePackageLockJson(filepath, appSettings);
                        componentsForBOM.AddRange(components);
                    }
                }
                GetdependencyDetails(componentsForBOM, dependencies);

            }
            else
            {
                bom = ParseCycloneDXBom(appSettings.CycloneDxBomFilePath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = bom.Components.Count;
                bom = RemoveExcludedComponents(appSettings, bom);

                componentsForBOM = bom.Components;
                dependencies = bom.Dependencies;
            }
      
        }

        public static void GetdependencyDetails(List<Component> componentsForBOM, List<Dependency> dependencies)
        {
            List<Dependency> dependencyList = new();
        
            foreach (var component in componentsForBOM)
            {
                if ((component.Author?.Split(",")) != null)
                {
                    List<Dependency> subDependencies = new();
                    foreach (var item in (component?.Author?.Split(",")).Where(item => item.Contains(":")))
                    {
                      
                        var componentDetails = item.Split(":");
                        var name = StringFormat(componentDetails[0]);
                        var version = StringFormat(componentDetails[1]);
                        string purlId = $"{ApiConstant.NPMExternalID}{name}@{version}";
                        Dependency dependentList = new Dependency()
                        {
                            Ref = purlId
                        };
                        subDependencies.Add(dependentList);
                    }

                    var dependency = new Dependency()
                    {
                        Ref = component.Purl,
                        Dependencies = subDependencies
                    };

                    dependencyList.Add(dependency);

                    component.Author = "";

                }
            }
            dependencies.AddRange(dependencyList);
        }

        private static string StringFormat(string componentInfo)
        {
            var replacements = new Dictionary<string, string> { { "@", "%40" }, { "\"", "" }, { "{", "" }, { "\r", "" }, { "}", "" }, { "\n", "" } };

            var formattedstring = replacements.Aggregate(componentInfo, (current, replacement) => current.Replace(replacement.Key, replacement.Value));
            return formattedstring.Trim();
        }

        private static bool IsDevDependency( JToken devValue, ref int noOfDevDependent)
        {
            if (devValue != null)
            {
                noOfDevDependent++;
            }

            return devValue != null;
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

        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.NpmPackage))
                {
                    components.Add(componentsInfo);
                    Logger.Debug($"GetExcludedComponentsList():ValidComponent For NPM : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For NPM : Component Details : {componentsInfo?.Name} @ {componentsInfo?.Version} @ {componentsInfo?.Purl}");
                }
            }
            return components;
        }
    }
}
