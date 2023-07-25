// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    public class MavenProcessor : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";



        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new();
            List<Component> componentsToBOM = new();
            Bom bom = new();

            List<string> configFiles;
            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {
                configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Maven);
            }
            else
            {
                configFiles = FolderScanner.FileScanner(appSettings.CycloneDxBomFilePath, appSettings.Maven);
            }


            foreach (string filepath in configFiles)
            {
                Bom bomList = ParseCycloneDXBom(filepath);
                DevDependencyIdentification( componentsForBOM, bomList,ref componentsToBOM);
                componentsForBOM.AddRange(bomList.Components);
                SetPropertiesforBOM(componentsForBOM, componentsToBOM);

            }
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count;

            int totalComponentsIdentified = componentsForBOM.Count;

            //Removing if there are any other duplicates           
            componentsForBOM = componentsToBOM.Distinct(new ComponentEqualityComparer()).ToList();

            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;


            bom.Components = componentsForBOM;

            BomCreator.bomKpiData.ComponentsInComparisonBOM = bom.Components.Count;
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }

        private void SetPropertiesforBOM(List<Component> componentsForBOM, List<Component> componentsToBOM)
        {
            if (componentsToBOM.Count == 0&& componentsForBOM.Count!=0)
            {
                foreach (var entry in componentsForBOM)
                {
                    SetPropertiesforBOM(ref componentsToBOM, entry, "false");
                }
            }
          
        }

        private static void SetPropertiesforBOM(ref List<Component> componentsToBOM, Component component, string devValue)
        {
        
                component.Properties = new List<Property>();
                Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = devValue };
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = "Discovered" };
                component.Properties.Add(isDev);
                component.Properties.Add(identifierType);
                componentsToBOM.Add(component);
      
        }

        private static void DevDependencyIdentification( List<Component> componentsForBOM, Bom bomList,ref List<Component> componentsToBOM)
        {
            List<Component> componentList = bomList.Components;

            if (componentsForBOM?.Count >= componentList?.Count && componentsForBOM.Count != 0)
            {
                foreach (var entry in componentsForBOM)
                {
                    if (componentList.Exists(x => x.Name == entry.Name))
                    {
                        SetPropertiesforBOM(ref componentsToBOM, entry, "false");
                    }
                    else
                    {
                        SetPropertiesforBOM(ref componentsToBOM, entry, "true");

                        BomCreator.bomKpiData.DevDependentComponents++;
                    }
                }
            }
            else if (componentsForBOM?.Count <= componentList?.Count && componentsForBOM.Count != 0)
            {
                foreach (var entry in componentList)
                {

                    if (componentsForBOM.Exists(x => x.Name == entry.Name))
                    {
                        SetPropertiesforBOM(ref componentsToBOM, entry, "false");
                    }
                    else
                    {
                        SetPropertiesforBOM(ref componentsToBOM, entry, "true");

                        BomCreator.bomKpiData.DevDependentComponents++;
                    }
                }
            }
            else
            {
             //do nothing
            }

        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Maven?.JfrogMavenRepoList, jFrogService);
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

            string repoName = aqlResultList.Find(x => x.Name.Contains(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";

            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) &&
                repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                repoName = aqlResultList.Find(x => x.Name.Contains(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;
            }

            return repoName;
        }
    }
}
