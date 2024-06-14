// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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

namespace LCT.PackageIdentifier
{

    /// <summary>
    /// Parses the Conan Packages
    /// </summary>
    public class ConanProcessor : CycloneDXBomParser,IParser
    {
        #region fields
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser;
        #endregion

        #region constructor
        public ConanProcessor(ICycloneDXBomParser cycloneDXBomParser)
        {
            _cycloneDXBomParser = cycloneDXBomParser;
        }
        #endregion

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM;
            Bom bom = new Bom();

            ParsingInputFileForBOM(appSettings, ref bom);
            componentsForBOM = bom.Components;

            componentsForBOM = GetExcludedComponentsList(componentsForBOM);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();

            var componentsWithMultipleVersions = componentsForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            if (componentsWithMultipleVersions.Count != 0)
            {
                CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);
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
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = appSettings.InternalRepoList.Concat(appSettings.Conan?.JfrogConanRepoList).ToArray();
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                string repoName = GetArtifactoryRepoName(aqlResultList, component);
                string jfrogpackageName = $"{component.Name}-{component.Version}";
                var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoUrl, Value = repoName };
                Component componentVal = component;

                if (componentVal.Properties?.Count == null || componentVal.Properties?.Count <= 0)
                {
                    componentVal.Properties = new List<Property>();
                }
                componentVal.Properties.Add(artifactoryrepo);
                componentVal.Properties.Add(projectType);
                componentVal.Description = null;
                if (hashes!=null)
                {
                    componentVal.Hashes = new List<Hash>()
                {

                new()
                 {
                  Alg = Hash.HashAlgorithm.MD5,
                  Content = hashes.MD5
                },
                new()
                {
                  Alg = Hash.HashAlgorithm.SHA_1,
                  Content = hashes.SHA1
                 },
                 new()
                 {
                  Alg = Hash.HashAlgorithm.SHA_256,
                  Content = hashes.SHA256
                  }
                  };

                }
                modifiedBOM.Add(componentVal);
            }

            return modifiedBOM;
        }

        public static bool IsDevDependency(ConanPackage component, List<string> buildNodeIds, ref int noOfDevDependent)
        {
            var isDev = false;
            if (buildNodeIds != null && buildNodeIds.Contains(component.Id))
            {
                isDev = true;
                noOfDevDependent++;
            }

            return isDev;
        }

        #endregion

        #region private methods

        private static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            IFileOperations fileOperations = new FileOperations();
            string filename = $"{appSettings.BomFolderPath}\\{appSettings.SW360ProjectName}_{FileConstant.multipleversionsFileName}";
            if (string.IsNullOrEmpty(appSettings.IdentifierBomFilePath) || (!File.Exists(filename)))
            {
                multipleVersions.Conan = new List<MultipleVersionValues>();
                foreach (var conanPackage in componentsWithMultipleVersions)
                {
                    conanPackage.Description = !string.IsNullOrEmpty(appSettings.CycloneDxSBomTemplatePath) ? appSettings.CycloneDxSBomTemplatePath : conanPackage.Description;

                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = conanPackage.Name;
                    jsonComponents.ComponentVersion = conanPackage.Version;
                    jsonComponents.PackageFoundIn = conanPackage.Description;
                    multipleVersions.Conan.Add(jsonComponents);
                }
                fileOperations.WriteContentToMultipleVersionsFile(multipleVersions, appSettings.BomFolderPath, FileConstant.multipleversionsFileName, appSettings.SW360ProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {multipleVersions.Conan.Count} and details can be found at {appSettings.BomFolderPath}\\{appSettings.SW360ProjectName}_{FileConstant.multipleversionsFileName}\n");
            }
            else
            {
                string json = File.ReadAllText(filename);
                MultipleVersions myDeserializedClass = JsonConvert.DeserializeObject<MultipleVersions>(json);
                List<MultipleVersionValues> conanComponents = new List<MultipleVersionValues>();
                foreach (var conanPackage in componentsWithMultipleVersions)
                {
                    conanPackage.Description = !string.IsNullOrEmpty(appSettings.CycloneDxSBomTemplatePath) ? appSettings.CycloneDxSBomTemplatePath : conanPackage.Description;

                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = conanPackage.Name;
                    jsonComponents.ComponentVersion = conanPackage.Version;
                    jsonComponents.PackageFoundIn = conanPackage.Description;

                    conanComponents.Add(jsonComponents);
                }
                myDeserializedClass.Conan = conanComponents;

                fileOperations.WriteContentToMultipleVersionsFile(myDeserializedClass, appSettings.BomFolderPath, FileConstant.multipleversionsFileName, appSettings.SW360ProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {conanComponents.Count} and details can be found at {appSettings.BomFolderPath}\\{appSettings.SW360ProjectName}_{FileConstant.multipleversionsFileName}\n");
            }
        }
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref Bom bom)
        {
            List<string> configFiles;
            List<Dependency> dependencies = new List<Dependency>();
            List<Component> componentsForBOM = new List<Component>();
            configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Conan);

            foreach (string filepath in configFiles)
            {
                if (filepath.ToLower().EndsWith("conan.lock"))
                {
                    Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);
                    var components = ParsePackageLockJson(filepath, ref dependencies);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
                }
                else if (filepath.EndsWith(FileConstant.CycloneDXFileExtension) && !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():Found as CycloneDXFile");
                    bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                    CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                    componentsForBOM.AddRange(bom.Components);
                    GetDetailsforManuallyAddedComp(componentsForBOM);
                }
            }

            int initialCount = componentsForBOM.Count;
            GetDistinctComponentList(ref componentsForBOM);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - componentsForBOM.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count;
            bom.Components = componentsForBOM;

            if (bom.Dependencies != null)
            {
                bom.Dependencies.AddRange(dependencies);
            }
            else
            {
                bom.Dependencies = dependencies;
            }

            if (File.Exists(appSettings.CycloneDxSBomTemplatePath) && appSettings.CycloneDxSBomTemplatePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                //Adding Template Component Details
                Bom templateDetails;
                templateDetails = ExtractSBOMDetailsFromTemplate(_cycloneDXBomParser.ParseCycloneDXBom(appSettings.CycloneDxSBomTemplatePath));
                CheckValidComponentsForProjectType(templateDetails.Components, appSettings.ProjectType);
                SbomTemplate.AddComponentDetails(bom.Components, templateDetails);
            }            

            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
        }

        private static List<Component> ParsePackageLockJson(string filepath, ref List<Dependency> dependencies)
        {
            List<Component> lstComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;

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

                GetDependecyDetails(lstComponentForBOM, nodePackages, dependencies);

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

        private static void GetDependecyDetails(List<Component> componentsForBOM, List<ConanPackage> nodePackages, List<Dependency> dependencies)
        {
            foreach (Component component in componentsForBOM)
            {
                var node = nodePackages.Find(x => x.Reference.Contains($"{component.Name}/{component.Version}"));
                var dependencyNodes = new List<ConanPackage>();
                if (node.Dependencies != null && node.Dependencies.Count > 0)
                {
                    dependencyNodes.AddRange(nodePackages.Where(x => node.Dependencies.Contains(x.Id)).ToList());
                }
                if (node.DevDependencies != null && node.DevDependencies.Count > 0)
                {
                    dependencyNodes.AddRange(nodePackages.Where(x => node.DevDependencies.Contains(x.Id)).ToList());
                }
                var dependency = new Dependency();
                var subDependencies = componentsForBOM.Where(x => dependencyNodes.Exists(y => y.Reference.Contains($"{x.Name}/{x.Version}")))
                                        .Select(x => new Dependency { Ref = x.Purl }).ToList();

                dependency.Ref = component.Purl;
                dependency.Dependencies = subDependencies;

                if (subDependencies.Count > 0)
                {
                    dependencies.Add(dependency);
                }
            }
        }

        private static void GetPackagesForBom(ref List<Component> lstComponentForBOM, ref int noOfDevDependent, List<ConanPackage> nodePackages)
        {
            var rootNode = nodePackages.FirstOrDefault();
            if (rootNode != null && (!rootNode.Dependencies.Any() || rootNode.Dependencies == null))
            {
                throw new ArgumentNullException(nameof(nodePackages), "Dependency(requires) node name details not present in the root node.");
            }

            ConanPackage package = nodePackages.Where(x => x.Id == "0").FirstOrDefault();
            List<string> directDependencies = new List<string>();
            directDependencies.AddRange(package.Dependencies);
            directDependencies.AddRange(package.DevDependencies);

            // Ignoring the root node as it is the package information node and we are anyways considering all
            // nodes in the lock file.
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
                var buildNodeIds = GetBuildNodeIds(nodePackages);
                if (IsDevDependency(component, buildNodeIds, ref noOfDevDependent))
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

                Property siemensFileName = new Property()
                {
                    Name = Dataconstant.Cdx_Siemensfilename,
                    Value = component.Reference
                };
                var isDirect = directDependencies.Contains(component.Id) ? "true" : "false";
                Property siemensDirect = new Property()
                {
                    Name = Dataconstant.Cdx_SiemensDirect,
                    Value = isDirect
                };

                components.Type=Component.Classification.Library;
                components.Purl = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";
                components.BomRef = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
                components.Properties.Add(siemensFileName);
                lstComponentForBOM.Add(components);
            }
        }

        private static List<string> GetBuildNodeIds(List<ConanPackage> nodePackages)
        {
            return nodePackages
                    .Where(y => y.DevDependencies != null)
                    .SelectMany(y => y.DevDependencies)
                    .ToList();
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

            var aqllist = aqlResultList.FindAll(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);

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
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For CONAN : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
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

        private static void GetDistinctComponentList(ref List<Component> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.Purl }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        private static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            int noOfExcludedComponents = 0;
            if (appSettings.Conan.ExcludedComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings.Conan.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;
            }
            cycloneDXBOM.Components = componentForBOM;
            return cycloneDXBOM;
        }

        private static void GetDetailsforManuallyAddedComp(List<Component> componentsForBOM)
        {
            foreach (var component in componentsForBOM)
            {
                component.Properties = new List<Property>();
                Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                component.Properties.Add(isDev);
                component.Properties.Add(identifierType);
            }
        }

        #endregion
    }
}