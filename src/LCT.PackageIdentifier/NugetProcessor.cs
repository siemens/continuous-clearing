// --------------------------------------------------------------------------------------------------------------------
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
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.PackageIdentifier.Model.NugetModel;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
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
using Directory = System.IO.Directory;

namespace LCT.PackageIdentifier
{
    public class NugetProcessor : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private readonly ICycloneDXBomParser _cycloneDXBomParser;
        private List<Component> listOfInternalComponents = new List<Component>();
        public NugetProcessor(ICycloneDXBomParser cycloneDXBomParser)
        {
            _cycloneDXBomParser = cycloneDXBomParser;
        }

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            Logger.Debug($"ParsePackageFile():Starting to parse the package file for identifying nuget components.");
            List<Component> listComponentForBOM = new List<Component>();
            Bom bom = new Bom();

            ParsingInputFileForBOM(appSettings, ref listComponentForBOM, ref bom);
            var componentsWithMultipleVersions = bom.Components.GroupBy(s => s.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            CheckForMultipleVersions(appSettings, componentsWithMultipleVersions);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            Logger.Debug($"ParsePackageFile():Completed parsing the package file.");
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
                        LogHandlingHelper.BasicErrorHandling("Missing ID Attribute", "ParsePackageConfig()", $"ID attribute not found in file: {packagesFilePath}", $"Identify ID attribute in this file:{packagesFilePath}");
                        continue;
                    }

                    if (versionAttribute?.Value == null)
                    {
                        Logger.Error($"\t{packagesFilePath}: ID: '{idAttribute.Value}' version attribute not found.");
                        LogHandlingHelper.BasicErrorHandling("Missing Version Attribute", "ParsePackageConfig()", $"Version attribute not found for ID: '{idAttribute.Value}' in file: {packagesFilePath}", $"Identify version attribute in this file:{packagesFilePath}");
                        continue;
                    }
                    NugetPackage package = new NugetPackage()
                    {
                        ID = idAttribute.Value,
                        Version = versionAttribute.Value,
                        Filepath = packagesFilePath,
                        IsDev = isDev
                    };
                    nugetPackages.Add(package);
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("IOException", "ParsePackageConfig()", ex, $"File Path: {packagesFilePath}");
                Logger.Error($"IOException occurred while parsing the package file: {packagesFilePath}. Details: {ex.Message}");
            }
            catch (XmlSyntaxException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("XmlSyntaxException", "ParsePackageConfig()", ex, $"File Path: {packagesFilePath}");
                Logger.Error($"XML syntax error occurred while parsing the package file: {packagesFilePath}. Details: {ex.Message}");
            }
            return nugetPackages;
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
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while accessing .csproj files.", "Parsecsproj()", ex, "");
                Logger.Error($"IOException occurred while parsing .csproj files for dependencies. Details: {ex.Message}");
            }
            catch (XmlSyntaxException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while parsing XML in .csproj files.", "Parsecsproj()", ex, "");
                Logger.Error($"XmlSyntaxException occurred while parsing .csproj files for dependencies. Details: {ex.Message}");
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
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while processing ReferenceTagDetails.", "ReferenceTagDetails()", ex, "");
                Logger.Error($"ArgumentException occurred while getting ReferenceTagDetails. Details: {ex.Message}");
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
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Starting to retrieve JFrog repository details for components.\n");
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                string jfrogpackageName = $"{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
                var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);

                string jfrogRepoPath = string.Empty;
                AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out jfrogRepoPath);
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData.Repo };
                Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData?.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
                Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
                Component componentVal = component;
                if (artifactoryrepo.Value == appSettings.Nuget.DevDepRepo)
                {
                    BomCreator.bomKpiData.DevdependencyComponents++;
                }
                if (appSettings.Nuget.Artifactory.ThirdPartyRepos != null)
                {
                    foreach (var thirdPartyRepo in appSettings.Nuget.Artifactory.ThirdPartyRepos)
                    {
                        if (artifactoryrepo.Value == thirdPartyRepo.Name)
                        {
                            BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                            break;
                        }
                    }
                }
                if (artifactoryrepo.Value == appSettings.Nuget.ReleaseRepo)
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
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }

        public AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
                                             Component component,
                                             IBomHelper bomHelper, out string jfrogRepoPath)
        {
            AqlResult aqlResult = new AqlResult();
            jfrogRepoPath = string.Empty;
            string jfrogcomponentName = $"{component.Name}-{component.Version}.nupkg";

            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));
            if (aqlResults == null || aqlResults.Count <= 0)
            {
                jfrogcomponentName = $"{component.Name}.{component.Version}.nupkg";
                aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));
            }
            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);


            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = $"{fullName}-{component.Version}.nupkg";
                if (fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase))
                {
                    aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                        fullNameVersion, StringComparison.OrdinalIgnoreCase));
                    if (aqlResults == null || aqlResults.Count <= 0)
                    {
                        fullNameVersion = $"{fullName}.{component.Version}.nupkg";
                        aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                        jfrogcomponentName, StringComparison.OrdinalIgnoreCase));
                    }
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

        public string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }
            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
            ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Nuget.Artifactory.InternalRepos, jFrogService);

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
            listOfInternalComponents = internalComponents;
            Logger.Debug($"IdentificationOfInternalComponents(): identified internal components:{internalComponents.Count}.");
            Logger.Debug($"IdentificationOfInternalComponents(): Completed identification of internal components.\n");
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
            List<Dependency> dependenciesForBOM = cycloneDXBOM.Dependencies?.ToList() ?? new List<Dependency>();
            int noOfExcludedComponents = 0;
            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings?.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                dependenciesForBOM = CommonHelper.RemoveInvalidDependenciesAndReferences(componentForBOM, dependenciesForBOM);
                BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;

            }
            cycloneDXBOM.Components = componentForBOM;
            cycloneDXBOM.Dependencies = dependenciesForBOM;
            return cycloneDXBOM;
        }

        #endregion

        #region private methods
        private void ParsingInputFileForBOM(CommonAppSettings appSettings,
                                            ref List<Component> listComponentForBOM,
                                            ref Bom bom)
        {
            List<string> configFiles;
            List<Component> componentsForBOM = new List<Component>();
            List<Dependency> dependencies = new List<Dependency>();
            int totalComponentsIdentified = 0;

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Nuget);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                    Logger.Debug($"Template BOM file detected: {filepath}");
                }

                if (filepath.EndsWith(FileConstant.CycloneDXFileExtension))
                {
                    if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                    {
                        Logger.Debug($"ParsingInputFileForBOM():CycloneDX file detected: {filepath}");
                        bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                        CycloneDXBomParser.CheckValidComponentsForProjectType(
                            bom.Components, appSettings.ProjectType);
                        componentsForBOM.AddRange(bom.Components);
                        CommonHelper.GetDetailsforManuallyAdded(componentsForBOM,
                            listComponentForBOM);
                        LogHandlingHelper.IdentifierInputfileComponents(filepath, bom.Components);
                    }
                }
                else
                {
                    Logger.Debug($"ParsingInputFileForBOM():Package file detected: {filepath}");
                    List<NugetPackage> listofComponents = new();
                    ParseInputFiles(appSettings, filepath, listofComponents);
                    ConvertToCycloneDXModel(listComponentForBOM, listofComponents, dependencies);
                    if (bom.Dependencies == null || bom.Dependencies.Count == 0)
                    {
                        bom.Dependencies = dependencies;
                        dependencies = new List<Dependency>();
                    }
                    else
                    {
                        bom.Dependencies.AddRange(dependencies);
                    }
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile =
                        listComponentForBOM.Count;
                    LogHandlingHelper.IdentifierInputfileComponents(filepath, listComponentForBOM);
                }
            }

            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = listComponentForBOM.Count;
            totalComponentsIdentified = listComponentForBOM.Count;
            listComponentForBOM = KeepUniqueNonDevComponents(listComponentForBOM);
            listComponentForBOM = listComponentForBOM.Distinct(
                new ComponentEqualityComparer()).ToList();

            if (BomCreator.bomKpiData.DuplicateComponents == 0)
            {
                BomCreator.bomKpiData.DuplicateComponents =
                    totalComponentsIdentified - listComponentForBOM.Count;
            }

            BomCreator.bomKpiData.DevDependentComponents =
                listComponentForBOM.Count(s => s.Properties[0].Value == "true");
            bom.Components = listComponentForBOM;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);

            if (bom != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
        }

        public void AddSiemensDirectProperty(ref Bom bom)
        {
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };

                var isDirectDep = NugetDevDependencyParser.NugetDirectDependencies
                    .Exists(x => x.Contains(component.Name) && x.Contains(component.Version));

                if (isDirectDep) { siemensDirect.Value = "true"; }

                component.Properties ??= new List<Property>();

                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));

                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
        }

        private static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            IFileOperations fileOperations = new FileOperations();
            string defaultProjectName = CommonIdentiferHelper.GetDefaultProjectName(appSettings);
            string bomFullPath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_Bom.cdx.json";
            string filePath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_{FileConstant.multipleversionsFileName}";
            if (!File.Exists(filePath))
            {
                multipleVersions.Nuget = new List<MultipleVersionValues>();
                foreach (var nugetPackage in componentsWithMultipleVersions)
                {
                    nugetPackage.Description = !string.IsNullOrEmpty(bomFullPath) ? bomFullPath : nugetPackage.Description;
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = nugetPackage.Name;
                    jsonComponents.ComponentVersion = nugetPackage.Version;
                    jsonComponents.PackageFoundIn = nugetPackage.Description;
                    multipleVersions.Nuget.Add(jsonComponents);
                }
                fileOperations.WriteContentToMultipleVersionsFile(multipleVersions, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {multipleVersions.Nuget.Count} and details can be found at {filePath}\n");
            }
            else
            {
                string json = File.ReadAllText(filePath);
                MultipleVersions myDeserializedClass = JsonConvert.DeserializeObject<MultipleVersions>(json);
                List<MultipleVersionValues> nugetComponents = new List<MultipleVersionValues>();
                foreach (var nugetPackage in componentsWithMultipleVersions)
                {
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = nugetPackage.Name;
                    jsonComponents.ComponentVersion = nugetPackage.Version;
                    jsonComponents.PackageFoundIn = nugetPackage.Description;

                    nugetComponents.Add(jsonComponents);
                }
                myDeserializedClass.Nuget = nugetComponents;

                fileOperations.WriteContentToMultipleVersionsFile(myDeserializedClass, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {nugetComponents.Count} and details can be found at {filePath}\n");
            }
        }

        private static void ConvertToCycloneDXModel(List<Component> listComponentForBOM, List<NugetPackage> listofComponents, List<Dependency> dependencies)
        {
            foreach (var prop in listofComponents)
            {
                Component components = new Component
                {
                    Name = prop.ID,
                    Version = prop.Version,
                    Type = Component.Classification.Library
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
                        Name = Dataconstant.Cdx_IdentifierType,Value=Dataconstant.Discovered
                    }
                };
                listComponentForBOM.Add(components);
                if (prop.Dependencies != null)
                {
                    GetDependencyDetails(components, prop, ref dependencies);
                }
            }
        }

        private static void GetDependencyDetails(Component compnent, NugetPackage prop, ref List<Dependency> dependencies)
        {
            Logger.Debug($"GetDependencyDetails(): Starting dependency extraction for component: [Name: {compnent.Name}, Version: {compnent.Version}, PURL: {compnent.Purl}]");
            List<Dependency> subDependencies = new();
            foreach (var item in prop.Dependencies)
            {
                string purl = item;
                Logger.Debug($"GetDependencyDetails(): Extracted Sub-Dependency: [PURL: {purl}]");
                Dependency dependentList = new Dependency()
                {
                    Ref = purl
                };
                subDependencies.Add(dependentList);
            }
            var dependency = new Dependency()
            {
                Ref = compnent.Purl,
                Dependencies = subDependencies
            };
            Logger.Debug($"GetDependencyDetails(): Final Dependency for Component: [Ref: {dependency.Ref}, Sub-Dependencies: [{string.Join(", ", dependency.Dependencies.Select(d => d.Ref))}]]");
            dependencies.Add(dependency);
            Logger.Debug($"GetDependencyDetails(): Completed dependency extraction for component: [Name: {compnent.Name}, Version: {compnent.Version}, PURL: {compnent.Purl}]\n");
        }

        private static List<Component> KeepUniqueNonDevComponents(List<Component> listComponentForBOM)
        {
            Dictionary<string, Component> keyValuePairs = new Dictionary<string, Component>();
            foreach (var component in listComponentForBOM)
            {
                if (!keyValuePairs.ContainsKey(component.Purl))
                {
                    keyValuePairs.Add(component.Purl, component);
                }
                else
                {
                    if (keyValuePairs[component.Purl].Properties[0].Value == "true" && component.Properties[0].Value == "false")
                    {
                        //Already Comp with Development Dependent added as 'true' ,remove that Comp
                        //& add New Comp as Development Dependent 'false' If case of Duplicate
                        keyValuePairs.Remove(component.Purl);
                        keyValuePairs.Add(component.Purl, component);
                    }
                }
            }
            BomCreator.bomKpiData.DuplicateComponents = listComponentForBOM.Count - keyValuePairs.Values.Count;
            return keyValuePairs.Values.ToList();
        }

        private static void ParseInputFiles(CommonAppSettings appSettings, string filepath, List<NugetPackage> listofComponents)
        {
            if (filepath.EndsWith(FileConstant.NugetAssetFile))
            {
                listofComponents.AddRange(ParseAssetFile(filepath));
            }
            else if (filepath.EndsWith(".config"))
            {
                var list = ParsePackageConfig(filepath, appSettings);
                if (list != null)
                {
                    NugetDevDependencyParser.NugetDirectDependencies.AddRange(list?.Select(x => x.ID + " " + x.Version));
                }

                listofComponents.AddRange(list);
            }
            else
            {
                LogHandlingHelper.BasicErrorHandling("FileNotFound", "ParseInputFiles()", $"Input file not found or unsupported: {filepath}", $"provide {FileConstant.NugetAssetFile} or packages.config filesin this path :{filepath} ");
                Logger.Warn($"Input file NOT_FOUND :{filepath}");
            }
        }


        private static void CheckForMultipleVersions(CommonAppSettings appSettings, List<Component> componentsWithMultipleVersions)
        {

            if (componentsWithMultipleVersions.Count != 0)
            {
                CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);
            }
        }

        private static List<string> GetValidCsprojfile(CommonAppSettings appSettings)
        {
            List<string> allFoundCsprojFiles = new List<string>();
            string[] foundCsprojFiles = Directory.GetFiles(appSettings.Directory.InputFolder, "*.csproj", SearchOption.AllDirectories);
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
                LogHandlingHelper.ExceptionErrorHandling("Error occured while Extracting Library Details", "ExtractLibraryDetails()", ex, $"Library: {library}");
                version = "";
                return "";

            }
            //Invalid Package Details..
            catch (ArgumentException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Error occured while Extracting Library Details", "ExtractLibraryDetails()", ex, $"Library: {library}");
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

        private static List<NugetPackage> ParseAssetFile(string configFile)
        {
            NugetDevDependencyParser nugetDevDependencyParser = NugetDevDependencyParser.Instance;
            List<Container> containers = nugetDevDependencyParser.Parse(configFile);
            return ConvertContainerAsNugetPackage(containers, configFile);
        }

        private static List<NugetPackage> ConvertContainerAsNugetPackage(List<Container> containers, string configFile)
        {
            List<NugetPackage> nugetPackages = new List<NugetPackage>();
            foreach (Container containermodule in containers)
            {
                foreach (var lst in containermodule.Components)
                {
                    List<string> depvalue = new List<string>();
                    GetDependencyList(lst, ref depvalue);
                    nugetPackages.Add(new NugetPackage()
                    {
                        ID = lst.Value.Name,
                        Version = lst.Value.Version,
                        Dependencies = depvalue,
                        Filepath = configFile,
                        IsDev = lst.Value.Scope.ToString() == "DevDependency" ? "true" : "false",
                    });

                }
            }

            return nugetPackages;
        }
        public static void GetDependencyList(KeyValuePair<string, BuildInfoComponent> lst, ref List<string> depvalue)
        {
            if (lst.Value.Dependencies.Count > 0)
            {
                foreach (var item in lst.Value.Dependencies)
                {
                    var depvaltestue = item.PackageUrl;
                    depvalue.Add(depvaltestue);
                }
            }

        }
        #endregion
    }
}