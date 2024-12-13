// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    public class MavenProcessor : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        readonly CycloneDXBomParser cycloneDXBomParser;

        public MavenProcessor()
        {
            cycloneDXBomParser = new CycloneDXBomParser();
        }

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new();
            List<Component> componentsToBOM = new();
            List<Component> ListOfComponents = new();
            Bom bom = new();
            int noOfExcludedComponents = 0;
            List<Dependency> dependenciesForBOM = new();
            List<string> configFiles;

            configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Maven);

            foreach (string filepath in configFiles)
            {
                if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Bom bomList = ParseCycloneDXBom(filepath);
                    CheckValidComponentsForProjectType(bomList.Components, appSettings.ProjectType);

                    if (componentsForBOM.Count == 0)
                    {
                        componentsForBOM.AddRange(bomList?.Components);
                    }
                    else
                    {
                        componentsToBOM.AddRange(bomList?.Components);
                    }

                    if (bomList.Dependencies != null)
                    {
                        dependenciesForBOM.AddRange(bomList.Dependencies);
                    }
                }
            }

            if (File.Exists(appSettings.CycloneDxSBomTemplatePath) && appSettings.CycloneDxSBomTemplatePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                //Adding Template Component Details
                Bom templateDetails;
                templateDetails = ExtractSBOMDetailsFromTemplate(cycloneDXBomParser.ParseCycloneDXBom(appSettings.CycloneDxSBomTemplatePath));
                CheckValidComponentsForProjectType(templateDetails.Components, appSettings.ProjectType);
                SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);
            }

            //checking Dev dependency
            DevDependencyIdentificationLogic(componentsForBOM, componentsToBOM, ref ListOfComponents);

            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count + componentsToBOM.Count;

            int totalComponentsIdentified = BomCreator.bomKpiData.ComponentsinPackageLockJsonFile;

            //Removing if there are any other duplicates           
            componentsForBOM = ListOfComponents.Distinct(new ComponentEqualityComparer()).ToList();

            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;


            if (appSettings.Maven.ExcludedComponents != null)
            {
                componentsForBOM = CommonHelper.RemoveExcludedComponents(componentsForBOM, appSettings.Maven.ExcludedComponents, ref noOfExcludedComponents);
                dependenciesForBOM = CommonHelper.RemoveInvalidDependenciesAndReferences(componentsForBOM, dependenciesForBOM);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;
            }

            bom.Components = componentsForBOM;
            bom.Dependencies = dependenciesForBOM;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = bom.Components.Count;
            Logger.Debug($"ParsePackageFile():End");
            return bom;
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

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = appSettings.InternalRepoList.Concat(appSettings.Maven?.JfrogMavenRepoList).ToArray();
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
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

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
           ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.InternalRepoList, jFrogService);

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

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}";

            var aqlResults = aqlResultList.FindAll(x => x.Name.Contains(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";

            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) &&
                repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                var aqllist = aqlResultList.FindAll(x => x.Name.Contains(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase));

                repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);
            }

            return repoName;
        }
    }
}
