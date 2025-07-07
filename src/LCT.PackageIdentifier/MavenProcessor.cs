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
            List<string> configFiles;

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Maven);
            List<string> listOfTemplateBomfilePaths = GetTemplateBomFilePaths(configFiles);
            ProcessBomFiles(configFiles, componentsForBOM, componentsToBOM, dependenciesForBOM, appSettings);

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, componentsForBOM, appSettings.ProjectType);


            //checking Dev dependency
            DevDependencyIdentificationLogic(componentsForBOM, componentsToBOM, ref ListOfComponents);

            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count + componentsToBOM.Count;

            int totalComponentsIdentified = BomCreator.bomKpiData.ComponentsinPackageLockJsonFile;

            //Removing if there are any other duplicates           
            componentsForBOM = ListOfComponents.Distinct(new ComponentEqualityComparer()).ToList();

            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;


            if (appSettings.SW360?.ExcludeComponents != null)
            {
                componentsForBOM = CommonHelper.RemoveExcludedComponents(componentsForBOM, appSettings.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                dependenciesForBOM = CommonHelper.RemoveInvalidDependenciesAndReferences(componentsForBOM, dependenciesForBOM);
                BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;
            }

            bom.Components = componentsForBOM;
            bom.Dependencies = dependenciesForBOM;
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            BomCreator.bomKpiData.ComponentsInComparisonBOM = bom.Components.Count;
            Logger.Debug($"ParsePackageFile():End");

            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            RemoveTypeJarSuffix(bom);
            return bom;
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
        private void ProcessBomFiles(List<string> configFiles, List<Component> componentsForBOM, List<Component> componentsToBOM, List<Dependency> dependenciesForBOM, CommonAppSettings appSettings)
        {
            foreach (string filepath in configFiles)
            {
                if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Bom bomList;
                    if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                    {
                        bomList = _spdxBomParser.ParseSPDXBom(filepath);
                        if (bomList.Components != null)
                        {
                            CommonHelper.AddSpdxSBomFileNameProperty(ref bomList, filepath);
                        }
                        BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
                    }
                    else
                    {
                        bomList = ParseCycloneDXBom(filepath);
                    }

                    if (bomList?.Components != null)
                    {
                        CheckValidComponentsForProjectType(bomList.Components, appSettings.ProjectType);
                    }
                    else
                    {
                        Logger.Warn("No components found in the BOM file : " + filepath);
                        continue;
                    }

                    AddComponentsToBom(bomList, componentsForBOM, componentsToBOM, dependenciesForBOM);
                }                
            }
        }

        private static void AddComponentsToBom(Bom bomList, List<Component> componentsForBOM, List<Component> componentsToBOM, List<Dependency> dependenciesForBOM)
        {
            if (componentsForBOM.Count == 0)
            {
                componentsForBOM.AddRange(bomList.Components);
            }
            else
            {
                componentsToBOM.AddRange(bomList.Components);
            }

            if (bomList.Dependencies != null)
            {
                dependenciesForBOM.AddRange(bomList.Dependencies);
            }
        }

        private static List<string> GetTemplateBomFilePaths(List<string> configFiles)
        {
            List<string> listOfTemplateBomfilePaths = new();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                }
            }
            return listOfTemplateBomfilePaths;
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
            mavenDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
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
                component.Properties = new List<Property>();
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

                AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out string jfrogRepoPath);
                Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData?.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
                Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData?.Repo };

                Component componentVal = component;

                // Extract KPI update logic to helper method
                UpdateKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

                // Use common helper to set component properties and hashes
                CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, siemensfileNameProp, jfrogRepoPathProp, hashes);

                modifiedBOM.Add(componentVal);
            }

            return modifiedBOM;
        }        

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
           ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Maven.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalMavenComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;

            return componentData;
        }
        /// <summary>
        /// Updates KPI data based on the repository value
        /// </summary>
        /// <param name="repoValue">The repository value to check</param>
        /// <param name="appSettings">Application settings containing repository configurations</param>
        private static void UpdateKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Maven.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
                return;
            }

            if (appSettings.Maven.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Maven.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        return;
                    }
                }
            }

            if (repoValue == appSettings.Maven.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
                return;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
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
