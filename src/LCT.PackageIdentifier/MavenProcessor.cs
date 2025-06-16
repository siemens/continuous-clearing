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
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    public class MavenProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new();
            List<Component> componentsToBOM = new();
            List<Component> ListOfComponents = new();
            Bom bom = new();
            int noOfExcludedComponents = 0;
            List<Dependency> dependenciesForBOM = new();

            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Maven);
            List<string> listOfTemplateBomfilePaths = GetTemplateBomFilePaths(configFiles);

            ProcessBomFiles(configFiles, ref componentsForBOM, ref componentsToBOM, ref dependenciesForBOM, appSettings);

            ProcessTemplateFile(listOfTemplateBomfilePaths, componentsForBOM, appSettings);

            IdentifyDevDependencies(componentsForBOM, componentsToBOM, ref ListOfComponents);

            RemoveDuplicates(ref componentsForBOM, ListOfComponents);

            ExcludeComponents(appSettings, ref componentsForBOM, ref dependenciesForBOM, ref noOfExcludedComponents);

            FinalizeBom(ref bom, componentsForBOM, dependenciesForBOM);

            Logger.Debug($"ParsePackageFile():End");

            return bom;
        }

        private static List<string> GetTemplateBomFilePaths(List<string> configFiles)
        {
            return configFiles.Where(filepath => filepath.EndsWith(FileConstant.SBOMTemplateFileExtension)).ToList();
        }

        private void ProcessBomFiles(List<string> configFiles, ref List<Component> componentsForBOM, ref List<Component> componentsToBOM, ref List<Dependency> dependenciesForBOM, CommonAppSettings appSettings)
        {
            foreach (string filepath in configFiles.Where(filepath => !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension)))
            {
                Bom bomList = ParseBomFile(filepath);
                if (bomList?.Components != null)
                {
                    CheckValidComponentsForProjectType(bomList.Components, appSettings.ProjectType);
                    AddComponentsToBom(bomList, ref componentsForBOM, ref componentsToBOM);
                    AddDependenciesToBom(bomList, ref dependenciesForBOM);
                }
                else
                {
                    Logger.Warn($"No components found in the BOM file: {filepath}");
                }
            }
        }

        private Bom ParseBomFile(string filepath)
        {
            Bom bomList;
            if (filepath.EndsWith(FileConstant.SPDXFileExtension))
            {
                bomList = _spdxBomParser.ParseSPDXBom(filepath);
                if (bomList != null)
                {
                    CommonHelper.AddSpdxSBomFileNameProperty(ref bomList, filepath);
                }
            }
            else
            {
                bomList = ParseCycloneDXBom(filepath);
            }
            return bomList;
        }

        private static void AddComponentsToBom(Bom bomList, ref List<Component> componentsForBOM, ref List<Component> componentsToBOM)
        {
            if (componentsForBOM.Count == 0)
            {
                componentsForBOM.AddRange(bomList.Components);
            }
            else
            {
                componentsToBOM.AddRange(bomList.Components);
            }
        }

        private static void AddDependenciesToBom(Bom bomList, ref List<Dependency> dependenciesForBOM)
        {
            if (bomList.Dependencies != null)
            {
                dependenciesForBOM.AddRange(bomList.Dependencies);
            }
        }

        private void ProcessTemplateFile(List<string> listOfTemplateBomfilePaths, List<Component> componentsForBOM, CommonAppSettings appSettings)
        {
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, componentsForBOM, appSettings.ProjectType);
        }

        private static void IdentifyDevDependencies(List<Component> componentsForBOM, List<Component> componentsToBOM, ref List<Component> ListOfComponents)
        {
            DevDependencyIdentificationLogic(componentsForBOM, componentsToBOM, ref ListOfComponents);
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count + componentsToBOM.Count;
        }

        private static void RemoveDuplicates(ref List<Component> componentsForBOM, List<Component> ListOfComponents)
        {
            int totalComponentsIdentified = BomCreator.bomKpiData.ComponentsinPackageLockJsonFile;
            componentsForBOM = ListOfComponents.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;
        }

        private static void ExcludeComponents(CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref List<Dependency> dependenciesForBOM, ref int noOfExcludedComponents)
        {
            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                componentsForBOM = CommonHelper.RemoveExcludedComponents(componentsForBOM, appSettings.SW360.ExcludeComponents, ref noOfExcludedComponents);
                dependenciesForBOM = CommonHelper.RemoveInvalidDependenciesAndReferences(componentsForBOM, dependenciesForBOM);
                BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;
            }
        }

        private static void FinalizeBom(ref Bom bom, List<Component> componentsForBOM, List<Dependency> dependenciesForBOM)
        {
            bom.Components = componentsForBOM;
            bom.Dependencies = dependenciesForBOM?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            BomCreator.bomKpiData.ComponentsInComparisonBOM = bom.Components.Count;

            AddSiemensDirectProperty(ref bom);
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            RemoveTypeJarSuffix(bom);
        }
        private static void RemoveTypeJarSuffix(Bom bom)
        {
            const string suffix = Dataconstant.TypeJarSuffix;

            foreach (var component in bom?.Components ?? Enumerable.Empty<Component>())
            {
                component.BomRef = RemoveSuffix(component.BomRef, suffix);
                component.Purl = RemoveSuffix(component.Purl, suffix);
            }

            foreach (var dependency in bom?.Dependencies ?? Enumerable.Empty<Dependency>()) 
            {
                RemoveTypeJarSuffixFromDependency(dependency);
            }
        }
        private static void RemoveTypeJarSuffixFromDependency(Dependency dependency)
        {
            const string suffix = Dataconstant.TypeJarSuffix;
            if (!string.IsNullOrEmpty(dependency.Ref) && dependency.Ref.EndsWith(suffix))
            {
                dependency.Ref = dependency.Ref[..^suffix.Length];
            }

            // Recursively process nested dependencies
            if (dependency.Dependencies != null && dependency.Dependencies.Count != 0)
            {
                foreach (var nestedDependency in dependency.Dependencies)
                {
                    RemoveTypeJarSuffixFromDependency(nestedDependency);
                }
            }
        }
        private static string RemoveSuffix(string value, string suffix)
        {
            return !string.IsNullOrEmpty(value) && value.EndsWith(suffix)
                ? value[..^suffix.Length]
                : value;
        }
        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            List<string> mavenDirectDependencies = new List<string>();
            mavenDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref)?.ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };
                if (mavenDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version)))
                {
                    siemensDirect.Value = "true";
                }

                component.Properties ??= new List<Property>();
                bool isPropExists = component.Properties.Exists(x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));
                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
        }

        public static void DevDependencyIdentificationLogic(List<Component> componentsForBOM, List<Component> componentsToBOM, ref List<Component> ListOfComponents)
        {

            List<Component> iterateBOM = componentsForBOM.Count > componentsToBOM.Count ? componentsForBOM : componentsToBOM;
            List<Component> checkBOM = componentsForBOM.Count < componentsToBOM.Count ? componentsForBOM : componentsToBOM;


            ListOfComponents = DevdependencyIdentification(ListOfComponents, iterateBOM, checkBOM);

        }

        private static List<Component> DevdependencyIdentification(List<Component> ListOfComponents, List<Component> iterateBOM, List<Component> checkBOM)
        {
            foreach (var item in iterateBOM)
            {
                //check to see if the second list is empty(which means customer has only provided one bom file)no dev dependency will be identified here
                if (checkBOM.Count == 0)
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, "false");
                }
                else if (checkBOM.Exists(x => x.Name == item.Name && x.Version == item.Version)) //check t see if both list has common elements
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, "false");
                }
                else //incase one list has a component not present in another then it will be marked as Dev
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, "true");

                    BomCreator.bomKpiData.DevDependentComponents++;
                }
            }

            return ListOfComponents;
        }

        private static void SetPropertiesforBOM(ref List<Component> componentsToBOM, Component component, string devValue)
        {
            Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
            Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = devValue };

            if (CommonHelper.ComponentPropertyCheck(component, Dataconstant.Cdx_IdentifierType))
            {
                if (!CommonHelper.ComponentPropertyCheck(component, Dataconstant.Cdx_IsDevelopment))
                {
                    component.Properties.Add(isDev);
                    componentsToBOM.Add(component);
                }
                else
                {
                    componentsToBOM.Add(component);
                }
            }
            else
            {
                component.Properties ??= new List<Property>();
                component.Properties.Add(isDev);
                component.Properties.Add(identifierType);
                componentsToBOM.Add(component);
            }
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
                                                                           CommonAppSettings appSettings,
                                                                           IJFrogService jFrogService,
                                                                           IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                string jfrogpackageName = $"{component.Name}-{component.Version}{ApiConstant.MavenExtension}";
                var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);

                string jfrogRepoPath = string.Empty;
                string jfrogcomponentName = $"{component.Name}-{component.Version}.jar";
                AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out jfrogRepoPath);
                Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData?.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
                Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData?.Repo };

                Component componentVal = component;
                if (artifactoryrepo.Value == appSettings.Maven.DevDepRepo)
                {
                    BomCreator.bomKpiData.DevdependencyComponents++;
                }
                if (appSettings.Maven.Artifactory.ThirdPartyRepos != null)
                {
                    foreach (var thirdPartyRepo in appSettings.Maven.Artifactory.ThirdPartyRepos)
                    {
                        if (artifactoryrepo.Value == thirdPartyRepo.Name)
                        {
                            BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                            break;
                        }
                    }

                }
                if (artifactoryrepo.Value == appSettings.Maven.ReleaseRepo)
                {
                    BomCreator.bomKpiData.ReleaseRepoComponents++;
                }

                if (artifactoryrepo.Value == Dataconstant.NotFoundInJFrog || artifactoryrepo.Value == "")
                {
                    BomCreator.bomKpiData.UnofficialComponents++;
                }
                if (componentVal.Properties?.Count == null || componentVal.Properties?.Count <= 0)
                {
                    componentVal.Properties = new List<Property>();
                }
                componentVal.Properties.Add(artifactoryrepo);
                componentVal.Properties.Add(projectType);
                componentVal.Properties.Add(siemensfileNameProp);
                componentVal.Properties.Add(jfrogRepoPathProp);
                componentVal.Description = null;
                if (hashes != null)
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

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
           ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Maven.Artifactory.InternalRepos, jFrogService);

            // find the components in the list of internal components
            List<Component> internalComponents = new List<Component>();
            var internalComponentStatusUpdatedList = new List<Component>();
            var inputIterationList = componentData.comparisonBOMData;

            foreach (Component component in inputIterationList)
            {
                var currentIterationItem = component;
                bool isTrue = IsInternalMavenComponent(aqlResultList, currentIterationItem, bomhelper);
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

        private static bool IsInternalMavenComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}";
            if (aqlResultList.Exists(x => x.Name.Contains(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) && aqlResultList.Exists(
                x => x.Name.Contains(fullNameVersion, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
                                                                Component component,
                                                                IBomHelper bomHelper,
                                                                out string jfrogRepoPath)
        {
            AqlResult aqlResult = new AqlResult();
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogcomponentName = $"{component.Name}-{component.Version}.jar";

            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = $"{fullName}-{component.Version}.jar";
                if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase))
                {
                    aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                        fullNameVersion, StringComparison.OrdinalIgnoreCase));

                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }
            if (aqlResult != null)
            {
                aqlResult.Repo ??= repoName;
            }
            return aqlResult;
        }

        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }
    }
}
