// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using log4net;
using log4net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;


namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Parses the NPM Packages
    /// </summary>
    public class NpmProcessor : CycloneDXBomParser, IParser, IProcessor
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string Bundled = "bundled";
        private const string Dependencies = "dependencies";
        private const string Dev = "dev";
        private const string Version = "version";

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


        public async Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentNotForBOM = new List<Component>();
            UploadArgs uploadArgs;
            string releaseName = component.Name;
            string componentname = releaseName;
            HttpResponseMessage responseBody;
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            //Setting the upload Arguments
            uploadArgs = SetUploadArguments(component, ref releaseName);

            responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgs);
            //when the package is not found in jfrog
            if (responseBody.StatusCode == HttpStatusCode.NotFound)
            {
                string componentName = component.Name.ToLowerInvariant();
                responseBody = await jfrogApicommunication.CheckPackageAvailabilityInRepo(repo, componentName, component.Version);
            }

            //when package is found in jfrog
            if (responseBody.StatusCode == HttpStatusCode.OK)
            {
                if (!string.IsNullOrEmpty(component.Group))
                {
                    componentname = $"{component.Group}/{component.Name}";
                }
                string hashcode = BomHelper.GetHashCodeUsingNpmView(componentname, component.Version);
                string jfrogresponse = await responseBody.Content.ReadAsStringAsync();
                JfrogInfo jfrogInfo = JsonConvert.DeserializeObject<JfrogInfo>(jfrogresponse);

                string shaJfrog = jfrogInfo?.Checksum?.Sha1;
                if (hashcode == shaJfrog)
                {
                    componentNotForBOM.Add(component);

                }
            }
            //Incase of invalid token
            if (responseBody.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Logger.Log(null, Level.Warn, $"Provide a valid token for JFrog Artifactory to enable" +
                    $" the internal component identification", null);
                throw new UnauthorizedAccessException();
            }
            return componentNotForBOM;

        }

        private static UploadArgs SetUploadArguments(Component component, ref string releaseName)
        {
            UploadArgs uploadArgs;
            if (!string.IsNullOrEmpty(component.Group))
            {
                releaseName = $"{component.Group}/{component.Name}";
                uploadArgs = new UploadArgs()
                {
                    PackageName = releaseName,
                    ReleaseName = releaseName,
                    Version = component.Version
                };
            }
            else
            {
                uploadArgs = new UploadArgs()
                {
                    PackageName = releaseName,
                    ReleaseName = component.Name,
                    Version = component.Version
                };
            }

            return uploadArgs;
        }

        public async Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentForBOM = new List<Component>();
            string releaseName = component.Name;
            HttpResponseMessage responseBody;
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            if (!string.IsNullOrEmpty(component.Group))
            {
                releaseName = $"{component.Group}/{component.Name}";

            }
            UploadArgs uploadArgs = new UploadArgs()
            {
                PackageName = releaseName,
                ReleaseName = component.Name,
                Version = component.Version
            };

            responseBody = await jfrogApicommunication.GetPackageByPackageName(uploadArgs);
            if (responseBody.StatusCode == HttpStatusCode.NotFound)
            {
                string componentName = component.Name.ToLowerInvariant();
                responseBody = await jfrogApicommunication.CheckPackageAvailabilityInRepo(repo, componentName, component.Version);
            }
            if (responseBody.StatusCode == HttpStatusCode.OK)
            {
                CycloneBomProcessor.SetProperties(appSettings, component, ref componentForBOM, repo);
            }
            return componentForBOM;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings)
        {
            Logger.Debug("IdentificationOfInternalComponents started");
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
            }

            return componentData;
        }
        public static async Task<List<Component>> ComponentIdentification(List<Component> comparisonBOMData, CommonAppSettings appSettings)
        {

            List<Component> componentNotForBOM = new List<Component>();
            await DefinedParallel.ParallelForEachAsync(
                        comparisonBOMData,
                        async component =>
                        {
                            Logger.Debug($"ComponentIdentification for {component.Name}");

                            foreach (var repo in appSettings.InternalRepoList)
                            {
                                var item = await CheckPackageAvailability(appSettings, component, repo);
                                componentNotForBOM.AddRange(item);

                            }
                        });
            return componentNotForBOM;
        }

        public async Task<List<Component>> GetRepoDetails(List<Component> componentsForBOM, CommonAppSettings appSettings)
        {

            List<Component> modifiedBOM = new List<Component>();

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
        private async Task<List<Component>> AddPackageAvailability(CommonAppSettings appSettings, Component component)
        {
            List<Component> modifiedBOM = new List<Component>();
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };

            foreach (var item in appSettings?.Npm?.JfrogNpmRepoList)
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

        private static async Task<List<Component>> CheckPackageAvailability(CommonAppSettings appSettings, Component component, string repo)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey
            };
            IProcessor processor = new NpmProcessor();
            List<Component> componentNotForBOM = await processor.CheckInternalComponentsInJfrogArtifactory(appSettings, artifactoryUpload, component, repo);
            return componentNotForBOM;

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


    }
}
