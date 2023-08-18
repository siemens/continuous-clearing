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

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new();
            Bom bom = new();

            string depFilePath = "";
            int totalComponentsIdentified = 0;
            List<string> configFiles = new();
            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {
                //Create empty dependency list file
                if (!string.IsNullOrEmpty(appSettings.PackageFilePath))
                {
                    configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Maven);
                    depFilePath = Path.Combine(appSettings.PackageFilePath, "POMDependencies.txt");
                    File.Create(depFilePath).Close();
                }

                foreach (var bomFilePath in configFiles)
                {
                    Result result = BomHelper.GetDependencyList(bomFilePath, depFilePath);
                    if (result.ExitCode != 0)
                    {
                        Logger.Debug("Error in downloading maven packages");
                    }
                }

                ParseConfigFile(depFilePath, appSettings, ref componentsForBOM);

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
                        Logger.Warn($"Component Name : {item.Name}\nComponent Version : {item.Version}\nPackage Found in : {appSettings.PackageFilePath}\n");
                    }
                }
                bom.Components = componentsForBOM;
            }
            else
            {
                bom = ParseCycloneDXBom(appSettings.CycloneDxBomFilePath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = bom.Components.Count;
            }
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }

        private static void ParseConfigFile(string depFilePath, CommonAppSettings appSettings, ref List<Component> foundPackages)
        {
            string[] lines = File.ReadAllLines(depFilePath);
            int noOfExcludedComponents = 0;
            int totalComponenstinInputFile = 0;
            foreach (string line in lines)
            {
                Component component;
                string trimmedLine = line.Trim();

                if (trimmedLine != string.Empty && trimmedLine != "none" && trimmedLine != "The following files have been resolved:")
                {
                    totalComponenstinInputFile++;
                    //Example entry: org.mockito:mockito-core:jar:1.10.19:compile
                    string[] parts = trimmedLine.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    string scope = "";
                    bool isDevelopmentComponent;

                    scope = GetPackageDetails(parts, out component);

                    isDevelopmentComponent = GetDevDependentScopeList(appSettings, scope);

                    if (!component.Version.Contains("win") && !isDevelopmentComponent)
                    {
                        foundPackages.Add(component);
                    }
                    if (isDevelopmentComponent)
                    {
                        BomCreator.bomKpiData.DevDependentComponents++;
                    }
                }
            }
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = totalComponenstinInputFile;
            if (appSettings.Maven.ExcludedComponents != null)
            {
                foundPackages = CommonHelper.RemoveExcludedComponents(foundPackages, appSettings.Maven.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

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

        private static bool GetDevDependentScopeList(CommonAppSettings appSettings, string scope)
        {
            return appSettings.Maven.DevDependentScopeList?.Contains(scope) ?? false;
        }

        private static string GetPackageDetails(string[] parts, out Component component)
        {
            string scope = string.Empty;
            MavenPackage package;
            component = new Component();

            if (parts.Length == 5)
            {
                package = new()
                {
                    ID = parts[1],
                    Version = parts[3],
                    GroupID = parts[0].Replace('.', '/')
                };
                scope = parts[4];
                component.Name = package.ID;
                component.Version = package.Version;
                component.Group = package.GroupID;
                component.BomRef = $"pkg:maven/{component.Name}@{component.Version}";
                component.Purl = $"pkg:maven/{component.Name}@{component.Version}";
            }
            else if (parts.Length == 6)
            {
                package = new()
                {
                    ID = parts[1],
                    Version = $"{parts[4]}-{parts[3]}",
                    GroupID = parts[0].Replace('.', '/')
                };
                scope = parts[4];
                component.Name = package.ID;
                component.Version = package.Version;
                component.Group = package.GroupID;
                component.BomRef = $"pkg:maven/{component.Name}@{component.Version}";
                component.Purl = $"pkg:maven/{component.Name}@{component.Version}";
            }

            return scope;
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
