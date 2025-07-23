// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
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
using File = System.IO.File;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Parses the NPM Packages
    /// </summary>
    public class NpmProcessor(ICycloneDXBomParser cycloneDXBomParser,ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private readonly static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private const string Bundled = "bundled";
        private const string Dependencies = "dependencies";
        private const string Dev = "dev";
        private const string DevOptional = "devOptional";
        private const string Version = "version";
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private const string Requires = "requires";
        private const string Name = "name";

        public Bom ParsePackageFile(CommonAppSettings appSettings,ref Bom unSupportedBomList)
        {
            List<Component> componentsForBOM = new List<Component>();
            Bom bom = new Bom();
            List<Dependency> dependencies = new List<Dependency>();
            int totalComponentsIdentified = 0;
            int totalUnsupportedComponentsIdentified = 0;

            ParsingInputFileForBOM(appSettings, ref componentsForBOM, ref bom, ref dependencies);
            totalComponentsIdentified = componentsForBOM.Count;
            totalUnsupportedComponentsIdentified=ListUnsupportedComponentsForBom.Components.Count;
            componentsForBOM = GetExcludedComponentsList(componentsForBOM);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();
            ListUnsupportedComponentsForBom.Components=ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponentsIdentified - ListUnsupportedComponentsForBom.Components.Count;
            var componentsWithMultipleVersions = componentsForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            if (componentsWithMultipleVersions.Count != 0)
            {
                CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);
            }

            bom.Components = componentsForBOM;
            bom.Dependencies = dependencies;
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }


        public static List<Component> ParsePackageLockJson(string filepath, CommonAppSettings appSettings)
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
                    List<JToken> directDependenciesList = GetDirectDependenciesList(filepath);
                    IEnumerable<JProperty> depencyComponentList = dependencies.Children().OfType<JProperty>();
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM, ref noOfDevDependent, depencyComponentList, directDependenciesList);
                }

                // the below logic for angular 16+version due to package-lock.json file format change
                if (dependencies == null)
                {
                    var pacakages = jsonDeserialized["packages"];
                    if (pacakages?.Children() != null)
                    {
                        IEnumerable<JProperty> depencyComponentList = pacakages.Children().OfType<JProperty>();
                        GetPackagesForBom(filepath, ref bundledComponents, ref lstComponentForBOM,
                            ref noOfDevDependent, depencyComponentList);
                    }
                }

                if (appSettings?.SW360?.ExcludeComponents != null)
                {
                    lstComponentForBOM = CommonHelper.RemoveExcludedComponents(lstComponentForBOM, appSettings.SW360.ExcludeComponents, ref noOfExcludedComponents);
                    BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;

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

        private static List<JToken> GetDirectDependenciesList(string filepath)
        {            
            string jsonContent = File.ReadAllText(filepath);
            var jsonDeserialized = JObject.Parse(jsonContent);
            List<JToken> dependencies = jsonDeserialized[Dependencies]?.ToList() ?? new List<JToken>();
            List<JToken> devDependencies = jsonDeserialized["devDependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> directDependencies = new List<JToken>();
            directDependencies.AddRange(dependencies);
            directDependencies.AddRange(devDependencies);
            return directDependencies;
        }

        private static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            FileOperations fileOperations = new FileOperations();
            string defaultProjectName = CommonIdentiferHelper.GetDefaultProjectName(appSettings);
            string bomFullPath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_Bom.cdx.json";
            string filePath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_{FileConstant.multipleversionsFileName}";
            if (!File.Exists(filePath))
            {
                multipleVersions.Npm = new List<MultipleVersionValues>();
                foreach (var npmpackage in componentsWithMultipleVersions)
                {
                    npmpackage.Description = !string.IsNullOrEmpty(bomFullPath) ? bomFullPath : npmpackage.Description;
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = npmpackage.Name;
                    jsonComponents.ComponentVersion = npmpackage.Version;
                    jsonComponents.PackageFoundIn = npmpackage.Description;
                    multipleVersions.Npm.Add(jsonComponents);
                }
                fileOperations.WriteContentToMultipleVersionsFile(multipleVersions, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {multipleVersions.Npm.Count} and details can be found at {filePath}\n");
            }
            else
            {
                string json = File.ReadAllText(filePath);
                MultipleVersions myDeserializedClass = JsonConvert.DeserializeObject<MultipleVersions>(json);
                List<MultipleVersionValues> npmComponents = new List<MultipleVersionValues>();
                foreach (var npmpackage in componentsWithMultipleVersions)
                {
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = npmpackage.Name;
                    jsonComponents.ComponentVersion = npmpackage.Version;
                    jsonComponents.PackageFoundIn = npmpackage.Description;

                    npmComponents.Add(jsonComponents);
                }
                myDeserializedClass.Npm = npmComponents;

                fileOperations.WriteContentToMultipleVersionsFile(myDeserializedClass, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {npmComponents.Count} and details can be found at {filePath}\n");
            }
        }


        private static void GetPackagesForBom(string filepath, ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM, ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();
            var property2 = depencyComponentList.ToList()[0];
            var parsedContent = JObject.Parse(Convert.ToString(property2.Value));
            List<JToken> dep = parsedContent["dependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> devDep = parsedContent["devDependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> directDependencies = new List<JToken>();
            directDependencies.AddRange(dep);
            directDependencies.AddRange(devDep);

            foreach (JProperty prop in depencyComponentList.Skip(1))
            {
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                if (string.IsNullOrEmpty(prop.Name))
                {
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile--;
                    continue;
                }

                Component components = new Component() { Manufacturer = new OrganizationalEntity() };
                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM 
                // If package section has Dev or DevOptional as true , considering it as Dev Component
                if (IsDevDependency(prop.Value[Dev], ref noOfDevDependent) || IsDevDependency(prop.Value[DevOptional], ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                string folderPath = CommonHelper.TrimEndOfString(filepath, $"\\{FileConstant.PackageLockFileName}");
                string packageName = GetPackageName(properties, prop);

                string componentName = packageName.StartsWith('@') ? packageName.Replace("@", "%40") : packageName;

                SetComponentGroupAndName(components, packageName);

                components.Type = Component.Classification.Library;
                components.Description = folderPath;
                components.Version = Convert.ToString(properties[Version]);
                components.Manufacturer.BomRef = prop.Value[Dependencies]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";

                CheckAndAddToBundleComponents(bundledComponents, prop, components);
                string isDirect = GetIsDirect(directDependencies, prop);
                Property siemensDirect = new Property() { Name = Dataconstant.Cdx_SiemensDirect, Value = isDirect };
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
                lstComponentForBOM.Add(components);
                lstComponentForBOM = RemoveBundledComponentFromList(bundledComponents, lstComponentForBOM);
            }
        }

        private static void SetComponentGroupAndName(Component component, string packageName)
        {
            if (packageName.Contains('@'))
            {
                component.Group = packageName.Split('/')[0];
                component.Name = packageName.Split('/')[1];
            }
            else
            {
                component.Name = packageName;
            }
        }
        private static string GetPackageName(JObject properties, JProperty prop)
        {
            string packageName;
            if (properties[Name] != null)
            {
                packageName = Convert.ToString(properties[Name]);
            }
            else
            {
                packageName = CommonHelper.GetSubstringOfLastOccurance(prop.Name, $"node_modules/");
            }
            return packageName;
        }
        public static string GetIsDirect(List<JToken> directDependencies, JProperty prop)
        {
            string subvalue = CommonHelper.GetSubstringOfLastOccurance(prop.Name, $"node_modules/");
            foreach (var item in directDependencies)
            {
                string value = Convert.ToString(item) ?? string.Empty;
                if (value.Contains(subvalue))
                {
                    return "true";
                }
            }

            return "false";
        }
        private static void CheckAndAddToBundleComponents(List<BundledComponents> bundledComponents, JProperty prop, Component components)
        {
            if (prop.Value[Bundled] != null && (!bundledComponents.Any(x => x.Name == components.Name && x.Version.Equals(components.Version, StringComparison.OrdinalIgnoreCase))))
            {
                BundledComponents component = new() { Name = components.Name, Version = components.Version };
                bundledComponents.Add(component);
            }
        }


        private static void GetComponentsForBom(string filepath, CommonAppSettings appSettings,
            ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM,
            ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList, List<JToken> directDependenciesList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();

            foreach (JProperty prop in depencyComponentList)
            {
                Component components = new Component() { Manufacturer = new OrganizationalEntity() };
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM 
                // If package section has Dev or DevOptional as true , considering it as Dev Component 
                if (IsDevDependency(prop.Value[Dev], ref noOfDevDependent) || IsDevDependency(prop.Value[DevOptional], ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                IEnumerable<JProperty> subDependencyComponentList = prop.Value[Dependencies]?.OfType<JProperty>();
                if (subDependencyComponentList != null)
                {
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM,
                                        ref noOfDevDependent, subDependencyComponentList, directDependenciesList);
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
                components.Manufacturer.BomRef = prop.Value[Requires]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.Type = Component.Classification.Library;
                string isDirect = GetIsDirect(directDependenciesList, prop);
                Property siemensDirect = new Property() { Name = Dataconstant.Cdx_SiemensDirect, Value = isDirect };
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
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
                await bomhelper.GetNpmListOfComponentsFromRepo(appSettings.Npm.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalNpmComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;

            return componentData;
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetNpmListOfComponentsFromRepo(repoList, jFrogService);

            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            return modifiedBOM;
        }

        private static Component ProcessComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            string jfrogpackageName = bomhelper.GetFullNameOfComponent(component);

            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));
            string jfrogRepoPath = string.Empty;
            AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out jfrogRepoPath);
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData.Repo };

            Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
            Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdateKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, siemensfileNameProp, jfrogRepoPathProp, hashes);

            return componentVal;
        }

        private static void UpdateKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Npm.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }
            
            if (appSettings.Npm.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Npm.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }
            
            if (repoValue == appSettings.Npm.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM, 
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Npm);
            List<string> listOfTemplateBomfilePaths = new List<string>();

            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                    continue;
                }

                Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);
                ProcessFileBasedOnType(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies);
            }

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, componentsForBOM, appSettings.ProjectType);
        }

        private void ProcessFileBasedOnType(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            if (filepath.EndsWith(FileConstant.CycloneDXFileExtension))
            {
                ProcessCycloneDXFile(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies);
            }
            else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
            {
                ProcessSPDXFile(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies);
            }
            else
            {
                ProcessPackageFile(filepath, appSettings, ref componentsForBOM, ref dependencies);
            }
        }

        private void ProcessCycloneDXFile(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                return;
            }

            Logger.Debug($"ParsingInputFileForBOM():Found as CycloneDXFile");
            bom = ParseCycloneDXBom(filepath);

            if (bom.Components != null)
            {
                bom = RemoveExcludedComponents(appSettings, bom);
                CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                AddingIdentifierType(bom.Components, "CycloneDXFile", filepath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += bom.Components.Count;
                componentsForBOM.AddRange(bom.Components);
            }

            if (bom.Dependencies != null)
            {
                dependencies.AddRange(bom.Dependencies);
            }
        }

        private void ProcessSPDXFile(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            bom = _spdxBomParser.ParseSPDXBom(filepath);
            bom = RemoveExcludedComponents(appSettings, bom);
            SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType,ref listUnsupportedComponents);
            AddingIdentifierType(bom.Components, "SpdxFile", filepath);
            AddingIdentifierType(listUnsupportedComponents.Components, "SpdxFile", filepath);
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += bom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += listUnsupportedComponents.Components.Count;
            componentsForBOM.AddRange(bom.Components);
            dependencies.AddRange(bom.Dependencies);
            ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
            ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
        }

        private static void ProcessPackageFile(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref List<Dependency> dependencies)
        {
            Logger.Debug($"ParsingInputFileForBOM():Found as Package File");
            var components = ParsePackageLockJson(filepath, appSettings);
            AddingIdentifierType(components, "PackageFile", filepath);
            componentsForBOM.AddRange(components);
            GetDependencyDetails(components, dependencies);
        }

        public static void GetDependencyDetails(List<Component> componentsForBOM, List<Dependency> dependencies)
        {
            List<Dependency> dependencyList = new();

            foreach (var component in componentsForBOM)
            {
                if ((component.Manufacturer?.BomRef?.Split(",")) != null)
                {
                    List<Dependency> subDependencies = new();
                    foreach (var item in (component.Manufacturer?.BomRef?.Split(",")).Where(item => item.Contains(':')))
                    {
                        var componentDetails = item.Split(":");
                        string name;
                        string version;

                        if (componentDetails.Length >= 3 && componentDetails[2].Contains('@'))
                        {
                            var npmDetails = componentDetails[2].Split('@');
                            name = StringFormat(npmDetails[0]);
                            version = StringFormat(npmDetails[1]);
                        }
                        else
                        {
                            name = StringFormat(componentDetails[0]);
                            version = StringFormat(componentDetails[1]);
                        }
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

                    component.Manufacturer = null;

                }
                component.Manufacturer = null;
            }
            dependencies.AddRange(dependencyList);
        }

        private static string StringFormat(string componentInfo)
        {
            var replacements = new Dictionary<string, string> { { "@", "%40" }, { "\"", "" }, { "{", "" }, { "\r", "" }, { "}", "" }, { "\n", "" } };

            var formattedstring = replacements.Aggregate(componentInfo, (current, replacement) => current.Replace(replacement.Key, replacement.Value));
            return formattedstring.Trim();
        }

        private static bool IsDevDependency(JToken devValue, ref int noOfDevDependent)
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
                       (!bundledComponents.Any(x => x.Name == sub.Name && x.Version.Equals(version, StringComparison.InvariantCultureIgnoreCase))))
                    {
                        BundledComponents component = new() { Name = sub.Name, Version = version };
                        bundledComponents.Add(component);
                    }
                }
            }
        }

        private static List<Component> RemoveBundledComponentFromList(List<BundledComponents> bundledComponents, List<Component> lstComponentForBOM)
        {
            List<Component> components = [.. lstComponentForBOM];

            foreach (var componentsToBOM in lstComponentForBOM.Where(x => bundledComponents.Any(y => y.Name == x.Name &&
               y.Version.Equals(x.Version, StringComparison.InvariantCultureIgnoreCase))))
            {
                components.Remove(componentsToBOM);
            }
            return components;
        }

        private static bool IsInternalNpmComponent(
            List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = bomHelper.GetFullNameOfComponent(component);
            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogcomponentName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version)))
            {
                return true;
            }

            return false;
        }

        public static AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
                                                                Component component,
                                                                IBomHelper bomHelper,
                                                                out string jfrogRepoPath)
        {
            AqlResult aqlResult = new AqlResult();
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogpackageName = bomHelper.GetFullNameOfComponent(component);

            var aqlResults = aqlResultList.FindAll(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }

            if (aqlResult != null)
            {
                aqlResult.Repo ??= NotFoundInRepo;
            }
            return aqlResult;
        }

        public static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["NPM"]))
                {
                    components.Add(componentsInfo);
                    Logger.Debug($"GetExcludedComponentsList():ValidComponent For NPM : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }               
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For NPM : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
            }
            return components;
        }

        private static void AddingIdentifierType(List<Component> components, string identifiedBy,string filePath)
        {
            foreach (var component in components)
            {
                component.Properties ??= new List<Property>();

                Property isDev;
                Property identifierType;
                if (identifiedBy == "PackageFile")
                {
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                    component.Properties.Add(identifierType);
                }
                else
                {
                    string fileName = Path.GetFileName(filePath);
                    if (identifiedBy == "SpdxFile")
                    {
                        SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
                    }
                    else
                    {                        
                        identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };                        
                        component.Properties.Add(identifierType);
                    }
                    isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                    component.Properties.Add(isDev);
                }
            }
        }

    }
}
