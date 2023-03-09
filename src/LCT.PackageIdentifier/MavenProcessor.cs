// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    public class MavenProcessor : CycloneDXBomParser, IParser, IProcessor
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<Component> componentsForBOM = new();
            Bom bom = new();

            string depFilePath = "";
            int totalComponentsIdentified = 0;
            List<string> configFiles = new();

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
        public async Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentForBOM = new();

            string releaseName = component.Name;
            HttpResponseMessage responseBody;
            JfrogApicommunication jfrogApicommunication = new MavenJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            if (!string.IsNullOrEmpty(component.Group))
            {
                releaseName = $"{component.Group}";
            }
            UploadArgs uploadArgs = new()
            {
                PackageName = component.Name,
                ReleaseName = releaseName,
                Version = component.Version
            };

            responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgs);
            if (responseBody.StatusCode == HttpStatusCode.NotFound)
            {
                string componentName = component.Name.ToLowerInvariant();
                UploadArgs uploadArgument = new()
                {
                    PackageName = componentName,
                    ReleaseName = releaseName,
                    Version = component.Version
                };
                responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgument);
            }
            if (responseBody.StatusCode == HttpStatusCode.OK)
            {
                CycloneBomProcessor.SetProperties(appSettings, component, ref componentForBOM, repo);
            }
            return componentForBOM;
        }
        public async Task<List<Component>> GetRepoDetails(List<Component> componentsForBOM, CommonAppSettings appSettings)
        {

            List<Component> modifiedBOM = new();

            foreach (var component in componentsForBOM)
            {

                List<Component> repoInfoBOM = await AddPackageAvailability(appSettings, component);
                modifiedBOM.AddRange(repoInfoBOM);
                if (repoInfoBOM.Count == 0)
                {
                    CycloneBomProcessor.SetProperties(appSettings, component, ref modifiedBOM);

                }


            }
            return modifiedBOM;

        }
        private async Task<List<Component>> AddPackageAvailability(CommonAppSettings appSettings, Component component)
        {
            List<Component> modifiedBOM = new();
            ArtifactoryCredentials artifactoryUpload = new()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };


            foreach (var item in appSettings?.Maven?.JfrogMavenRepoList)
            {
                List<Component> componentsForBOM = await GetJfrogArtifactoryRepoInfo(appSettings, artifactoryUpload, component, item);
                if (componentsForBOM.Count > 0)
                {
                    modifiedBOM = componentsForBOM;
                    break;
                }

            }

            return modifiedBOM;
        }

        public async Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentNotForBOM = new();
            HttpResponseMessage responseBody;
            UploadArgs uploadArgs = new()
            {
                PackageName = component.Name,
                ReleaseName = component.Group,
                Version = component.Version
            };
            JfrogApicommunication jfrogApicommunication = new MavenJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgs);
            if (responseBody.StatusCode == HttpStatusCode.NotFound)
            {
                string componentName = component.Name.ToLowerInvariant();
                UploadArgs uploadArgument = new()
                {
                    PackageName = componentName,
                    ReleaseName = component.Group,
                    Version = component.Version
                };
                responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgument);
            }
            if (responseBody.StatusCode == HttpStatusCode.OK)
            {
                componentNotForBOM.Add(component);
            }

            if (responseBody.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Logger.Log(null, Level.Warn, $"Provide a valid token for JFrog Artifactory to enable" +
                    $" the internal component identification", null);
                throw new UnauthorizedAccessException();
            }
            return componentNotForBOM;

        }
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings)
        {

            List<Component> componentNotForBOM;
            if (appSettings.InternalRepoList != null && appSettings.InternalRepoList.Length > 0)
            {
                componentNotForBOM = await ComponentIdentification(componentData.comparisonBOMData, appSettings);
                foreach (var item in componentNotForBOM)
                {
                    Component component = componentData.comparisonBOMData.First(x => x.Name == item.Name && x.Version == item.Version);
                    componentData.comparisonBOMData.Remove(component);
                }
                componentData.internalComponents = componentNotForBOM;
                BomCreator.bomKpiData.InternalComponents = componentNotForBOM.Count;
            }

            return componentData;
        }
        public static async Task<List<Component>> ComponentIdentification(List<Component> comparisonBOMData, CommonAppSettings appSettings)
        {

            List<Component> componentNotForBOM = new();
            await DefinedParallel.ParallelForEachAsync(
                        comparisonBOMData,
                        async component =>
                        {

                            foreach (var repo in appSettings.InternalRepoList)
                            {
                                componentNotForBOM.AddRange(await CheckPackageAvailability(appSettings, component, repo));
                            }
                        });
            return componentNotForBOM;
        }
        private static async Task<List<Component>> CheckPackageAvailability(CommonAppSettings appSettings, Component component, string repo)
        {

            ArtifactoryCredentials artifactoryUpload = new()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };
            IProcessor processor = new MavenProcessor();
            List<Component> componentNotForBOM = await processor.CheckInternalComponentsInJfrogArtifactory(appSettings, artifactoryUpload, component, repo);
            return componentNotForBOM;

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

    }
}
