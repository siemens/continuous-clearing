// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LCT.PackageIdentifier
{
    public class NugetProcessor : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

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
            string isDev = "false";
            try
            {
                List<ReferenceDetails> referenceList = Parsecsproj(appSettings);
                XDocument packageFile = XDocument.Load(packagesFilePath, LoadOptions.SetLineInfo);
                IEnumerable<XElement> nodes = packageFile.Descendants("package");
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += nodes.Count();
                foreach (XElement element in nodes)
                {
                    XAttribute idAttribute = element.Attribute("id");
                    XAttribute versionAttribute = element.Attribute("version");
                    XAttribute devDependencyAttribute = element.Attribute("developmentDependency");
                    string name = (string)element.Attribute("id");
                    string version = (string)element.Attribute("version");

                    if (IsDevDependent(referenceList, name, version) || devDependencyAttribute?.Value != null)
                    {

                        BomCreator.bomKpiData.DevDependentComponents++;
                        isDev = "true";
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
                        Filepath = packagesFilePath,
                        IsDev= isDev
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
            string isDev = "false";
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
                            string id = dependencyToken.ToObject<JProperty>().Name;
                            string version = dependencyToken.First.Value<string>("resolved");
                            if (dependencyToken.First.Value<string>("type") == "Dev" || IsDevDependent(referenceList, id, version))
                            {
                               BomCreator.bomKpiData.DevDependentComponents++;                          
                                isDev = "true";
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
                                Filepath = packagesFilePath,
                                IsDev= isDev
                                
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

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Nuget?.JfrogNugetRepoList, jFrogService);
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

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}.tgz";

            string repoName = aqlResultList.Find(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}.tgz";

            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) &&
                repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                repoName = aqlResultList.Find(x => x.Name.Equals(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;
            }

            return repoName;
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
                bool isTrue = IsInternalNugetComponent(aqlResultList, currentIterationItem, bomhelper);
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

        private static bool IsInternalNugetComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}.{component.Version}.nupkg";
            if (aqlResultList.Exists(x => x.Name.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}.{component.Version}.nupkg";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) 
                && aqlResultList.Exists(
                x => x.Name.Equals(fullNameVersion, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
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
            List<Component> componentsForBOM=new List<Component>();
          
            if (string.IsNullOrEmpty(appSettings.CycloneDxBomFilePath))
            {
                configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Nuget);
                List<NugetPackage> listofComponents = new List<NugetPackage>();

                ParseInputFiles(appSettings, configFiles, listofComponents);

                ConvertToCycloneDXModel(listComponentForBOM, listofComponents);
            }
            else
            {
                configFiles = FolderScanner.FileScanner(appSettings.CycloneDxBomFilePath, appSettings.Nuget);
                foreach (string filepath in configFiles)
                {
                    componentsForBOM.AddRange(ParseCycloneDXBom(filepath));
                }
                foreach(var component in componentsForBOM)
                {
                    component.Properties = new List<Property>();
                    Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                    Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = "Manually Added" };
                    component.Properties.Add(isDev);
                    component.Properties.Add(identifierType);
                }
                bom.Components = componentsForBOM;
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
                    Version = prop.Version
                };

                components.Purl = $"{ApiConstant.NugetExternalID}{prop.ID}@{components.Version}";
                components.BomRef = $"{ApiConstant.NugetExternalID}{prop.ID}@{components.Version}";
                components.Description = prop.Filepath;
                components.Properties = new List<Property>()
                {
                    new()
                    {
                       Name = Dataconstant.Cdx_IsDevelopment, Value = prop.IsDev
                    },
                    new Property()
                    {
                        Name=Dataconstant.Cdx_IdentifierType,Value="Discovered"
                    }
                };
                

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
