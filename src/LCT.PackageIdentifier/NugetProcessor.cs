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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LCT.PackageIdentifier
{
    public class NugetProcessor : CycloneDXBomParser, IParser, IProcessor
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            Logger.Debug($"ParsePackageFile():Start");
            List<Component> listComponentForBOM = new List<Component>();
            Bom bom = new Bom();
            int totalComponentsIdentified = 0;
            int noOfExcludedComponents = 0;

            ParsingInputFileForBOM(appSettings, ref listComponentForBOM, ref bom);
            totalComponentsIdentified = listComponentForBOM.Count;

            listComponentForBOM = listComponentForBOM.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - listComponentForBOM.Count;

            var componentsWithMultipleVersions = listComponentForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            CheckForMultipleVersions(appSettings, ref listComponentForBOM, ref noOfExcludedComponents, componentsWithMultipleVersions);

            Logger.Debug($"ParsePackageFile():End");
            bom.Components = listComponentForBOM;
            return bom;
        }

        public static List<NugetPackage> ParsePackageConfig(string packagesFilePath, CommonAppSettings appSettings)
        {
            List<NugetPackage> nugetPackages = new List<NugetPackage>();
         
            try
            {
                List<ReferenceDetails> referenceList = Parsecsproj(appSettings);
                XDocument packageFile = XDocument.Load(packagesFilePath, LoadOptions.SetLineInfo);
                IEnumerable<XElement> nodes = packageFile.Descendants("package");
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += nodes.Count();
                foreach (XElement element in nodes)
                {
                    bool devdependencyFlag = false;
                    XAttribute idAttribute = element.Attribute("id");
                    XAttribute versionAttribute = element.Attribute("version");
                    XAttribute devDependencyAttribute = element.Attribute("developmentDependency");
                    string name = (string)element.Attribute("id");
                    string version = (string)element.Attribute("version");

              
                    if (IsDevDependent(referenceList, name, version) || devDependencyAttribute?.Value != null)
                    {

                        BomCreator.bomKpiData.DevDependentComponents++;
                        devdependencyFlag = true;
                    }

                    if (idAttribute?.Value == null)
                    {
                        Logger.Error($"\t{packagesFilePath}: ID attribute not found.");
                        continue;
                    }

                    if (versionAttribute?.Value == null)
                    {
                        Logger.Error($"\t{packagesFilePath}: ID: '{idAttribute.Value}' version attribute not found.");
                        continue;
                    }
                    NugetPackage package = new NugetPackage()
                    {
                        ID = idAttribute.Value,
                        Version = versionAttribute.Value,
                        Isdevdependent= devdependencyFlag,
                        Filepath = packagesFilePath
                    };
                    nugetPackages.Add(package);
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"ParsePackageFile():", ex);
            }
            catch (XmlSyntaxException ex)
            {
                Logger.Error($"ParsePackageFile():", ex);
            }
            return nugetPackages;

        }

        public static List<NugetPackage> ParsePackageLock(string packagesFilePath, CommonAppSettings appSettings)
        {
            List<NugetPackage> packageList = new List<NugetPackage>();
   
            try
            {
                List<ReferenceDetails> referenceList = Parsecsproj(appSettings);
                using (StreamReader r = new StreamReader(packagesFilePath))
                {
                    string json = r.ReadToEnd();
                    JObject jObject = JObject.Parse(json);

                    JToken jDependencies = jObject["dependencies"];

                    foreach (JToken targetVersion in jDependencies.Children())
                    {
                        foreach (JToken dependencyToken in targetVersion.Children().Children())
                        {
                            bool devdependencyFlag = false;
                            string id = dependencyToken.ToObject<JProperty>().Name;
                            string version = dependencyToken.First.Value<string>("resolved");
                            if (dependencyToken.First.Value<string>("type") == "Dev" || IsDevDependent(referenceList, id, version))
                            {
                                BomCreator.bomKpiData.DevDependentComponents++;
                                devdependencyFlag = true;
                            }
                            if (dependencyToken.First.Value<string>("type") == "Project" || string.IsNullOrEmpty(version) && string.IsNullOrEmpty(id))
                            {

                                continue;
                            }

                            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;

                            NugetPackage package = new NugetPackage()
                            {
                                ID = id,
                                Version = version,
                                Isdevdependent= devdependencyFlag,
                                Filepath = packagesFilePath
                            };
                            packageList.Add(package);
                        }
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Logger.Error($"ParsePackageFile():", ex);
            }
            catch (IOException ex)
            {
                Logger.Error($"ParsePackageFile():", ex);
            }

            return packageList;

        }
        public static bool IsDevDependent(List<ReferenceDetails> referenceDetails, string name, string version)
        {
            foreach (var item in referenceDetails)
            {
                if (item.Library == name && item.Version == version && item.Private)
                {

                    return true;
                }
            }
            return false;
        }
        public static List<ReferenceDetails> Parsecsproj(CommonAppSettings appSettings)
        {
            List<ReferenceDetails> referenceList = new List<ReferenceDetails>();

            try
            {
                List<string> foundCsprojFiles = GetValidCsprojfile(appSettings);
                XmlDocument doc = new XmlDocument();
                foreach (var csprojFile in foundCsprojFiles)
                {
                    doc.Load(csprojFile);
                    XmlNodeList orderNodes = doc.GetElementsByTagName("ItemGroup");

                    foreach (XmlNode node in orderNodes)
                    {
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            ReferenceDetails fileInfo = ReferenceTagDetails(childNode);

                            if (!string.IsNullOrEmpty(fileInfo.Library) && !string.IsNullOrEmpty(fileInfo.Version))
                            {
                                referenceList.Add(new ReferenceDetails()
                                {
                                    Library = fileInfo.Library,
                                    Version = fileInfo.Version,
                                    Private = fileInfo.Private
                                });
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"Error occured while parsing .csproj file for dependencies", ex);
            }
            catch (XmlSyntaxException ex)
            {
                Logger.Error($"Error occured while parsing .csproj file for dependencies:", ex);
            }
            return referenceList;
        }

        private static ReferenceDetails ReferenceTagDetails(XmlNode childNode)
        {
            string version = string.Empty, library = string.Empty;
            bool isPrivateRef = false;

            try
            {
                if (childNode.Name == "Reference")
                {
                    //For Library details present in Reference tag..
                    foreach (XmlNode mainNode in childNode.ChildNodes)
                    {
                        if (mainNode.Name == "HintPath" && !string.IsNullOrEmpty(mainNode.InnerText))
                        {
                            library = ExtractLibraryDetails(mainNode.InnerText, out version);
                        }
                    }
                }
                else if (childNode.Name == "PackageReference")
                {
                    //For Library details present in PackageReference tag..
                    library = ReferenceTagDetailsForPackageReference(childNode, out version, out isPrivateRef);
                }
                else
                {
                    //do nothing
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Error occured while getting ReferenceTagDetails:", ex);
            }

            ReferenceDetails fileInfo = new ReferenceDetails()
            {
                Library = library,
                Version = version,
                Private = isPrivateRef
            };

            return fileInfo;
        }

        public async Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentForBOM = new List<Component>();

            string releaseName = component.Name;
            HttpResponseMessage responseBody;
            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            if (component.Name.Contains('/'))
            {
                releaseName = component.Name[(component.Name.IndexOf("/") + 1)..];
            }
            UploadArgs uploadArgs = new UploadArgs()
            {
                PackageName = component.Name,
                ReleaseName = releaseName,
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

        public async Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {

            List<Component> componentNotForBOM = new List<Component>();
            HttpResponseMessage responseBody;

            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication(appSettings.JFrogApi, repo, artifactoryUpload);
            responseBody = await jfrogApicommunication.CheckPackageAvailabilityInRepo(repo, component.Name, component.Version);
            if (responseBody.StatusCode == HttpStatusCode.NotFound)
            {
                string componentName = component.Name.ToLowerInvariant();
                responseBody = await jfrogApicommunication.CheckPackageAvailabilityInRepo(repo, componentName, component.Version);
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
            List<Component> componentNotForBOM = new List<Component>();
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

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            int noOfExcludedComponents = 0;
            if (appSettings.Nuget.ExcludedComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings.Nuget.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

            }
            cycloneDXBOM.Components = componentForBOM;
            return cycloneDXBOM;
        }


        #endregion
        #region private methods
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> listComponentForBOM, ref Bom bom)
        {
            List<string> configFiles;
            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {

                configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Nuget);
                List<NugetPackage> listofComponents = new List<NugetPackage>();

                ParseInputFiles(appSettings, configFiles, listofComponents);

                ConvertToCycloneDXModel(listComponentForBOM, listofComponents);

            }
            else
            {
                bom = ParseCycloneDXBom(appSettings.CycloneDxBomFilePath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = bom.Components.Count;
                bom = RemoveExcludedComponents(appSettings, bom);
                listComponentForBOM = bom.Components;
            }


        }

        private static void ConvertToCycloneDXModel(List<Component> listComponentForBOM, List<NugetPackage> listofComponents)
        {
            foreach (var prop in listofComponents)
            {

                Component components = new Component
                {
                    Name = prop.ID,
                    Version = prop.Version,
                    Cpe = prop.Isdevdependent.ToString().ToLower()
                };

                components.Purl = $"{ApiConstant.NugetExternalID}{prop.ID}@{components.Version}";
                components.BomRef = $"{ApiConstant.NugetExternalID}{prop.ID}@{components.Version}";
                components.Description = prop.Filepath;
                listComponentForBOM.Add(components);
            }
        }

        private static void ParseInputFiles(CommonAppSettings appSettings, List<string> configFiles, List<NugetPackage> listofComponents)
        {
            foreach (string filepath in configFiles)
            {
                if (filepath.Contains(".json"))
                {
                    listofComponents.AddRange(ParsePackageLock(filepath, appSettings));

                }
                else
                {
                    listofComponents.AddRange(ParsePackageConfig(filepath, appSettings));
                }

            }
        }

        private static void CheckForMultipleVersions(CommonAppSettings appSettings, ref List<Component> listComponentForBOM, ref int noOfExcludedComponents, List<Component> componentsWithMultipleVersions)
        {
            if (appSettings.Nuget.ExcludedComponents != null)
            {
                listComponentForBOM = CommonHelper.RemoveExcludedComponents(listComponentForBOM, appSettings.Nuget.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;
            }

            if (componentsWithMultipleVersions.Count != 0)
            {
                Logger.Warn($"Multiple versions detected :\n");
                foreach (var item in componentsWithMultipleVersions)
                {
                    item.Description = !string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath) ? appSettings.CycloneDxBomFilePath : item.Description;
                    Logger.Warn($"Component Name : {item.Name}\nComponent Version : {item.Version}\nPackage Found in : {item.Description}\n");
                }
            }
        }
        private static List<string> GetValidCsprojfile(CommonAppSettings appSettings)
        {
            List<string> allFoundCsprojFiles = new List<string>();
            string[] foundCsprojFiles = Directory.GetFiles(appSettings.PackageFilePath, "*.csproj", SearchOption.AllDirectories);
            if (foundCsprojFiles != null)
            {
                foreach (string csprojFile in foundCsprojFiles)
                {
                    if (!FolderScanner.IsExcluded(csprojFile, appSettings.Nuget?.Exclude))
                    {
                        allFoundCsprojFiles.Add(csprojFile);
                    }
                    else
                    {
                        Logger.Debug($"\tSkipping '{csprojFile}' due to exclusion pattern.");
                    }
                }
            }
            return allFoundCsprojFiles;
        }
        private static async Task<List<Component>> CheckPackageAvailability(CommonAppSettings appSettings, Component component, string repo)
        {

            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };
            IProcessor processor = new NugetProcessor();
            List<Component> componentNotForBOM = await processor.CheckInternalComponentsInJfrogArtifactory(appSettings, artifactoryUpload, component, repo);
            return componentNotForBOM;

        }
        private async Task<List<Component>> AddPackageAvailability(CommonAppSettings appSettings, Component component)
        {
            List<Component> modifiedBOM = new List<Component>();
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };


            foreach (var item in appSettings?.Nuget?.JfrogNugetRepoList)
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
        private static string ExtractLibraryDetails(string library, out string version)
        {
            try
            {
                var packageDetails = Regex.Match(library, @"packages\\(.+?)\\lib").Groups[1].Value;

                Match m = Regex.Match(packageDetails, @"\d+");
                if (m.Success)
                    version = packageDetails[m.Index..];
                else
                    version = "";
                library = packageDetails.Replace(version, "");


            }

            //Invalid Package Details..
            catch (RegexMatchTimeoutException ex)
            {
                Logger.Debug($"Error occured while Extracting Library Details:", ex);
                version = "";
                return "";

            }
            //Invalid Package Details..
            catch (ArgumentException ex)
            {
                Logger.Debug($"Error occured while Extracting Library Details:", ex);
                version = "";
                return "";

            }
            return library.Remove(library.Length - 1, 1);
        }
        private static string ReferenceTagDetailsForPackageReference(XmlNode childNode, out string version, out bool isPrivateRef)
        {
            string library = string.Empty;
            version = "";
            isPrivateRef = false;

            foreach (XmlAttribute att in childNode.Attributes)
            {

                if (att.Name == "Include")
                { library = att.Value; }

                else if (att.Name == "Version")
                {
                    version = att.Value;
                }
                else
                {
                    // do nothing
                }
            }
            foreach (XmlNode privateNode in childNode.ChildNodes)
            {
                if (privateNode.Name == "PrivateAssets")
                    isPrivateRef = true;
            }

            return library;
        }
        #endregion

    }
}
