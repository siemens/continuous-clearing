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
using NuGet.ProjectModel;
using NuGet.Versioning;
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
using File = System.IO.File;

namespace LCT.PackageIdentifier
{
    public partial class NugetProcessor(ICycloneDXBomParser cycloneDXBomParser, IFrameworkPackages frameworkPackages, ICompositionBuilder compositionBuilder,ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private readonly IFrameworkPackages _frameworkPackages = frameworkPackages;
        private readonly ICompositionBuilder _compositionBuilder = compositionBuilder;
        private Dictionary<string, Dictionary<string, NuGetVersion>> _listofFrameworkPackages = new Dictionary<string, Dictionary<string, NuGetVersion>>();
        private readonly Dictionary<string, Dictionary<string, NuGetVersion>> _listofFrameworkPackagesInInputFiles = new Dictionary<string, Dictionary<string, NuGetVersion>>();
        private bool isSelfContainedProject = false;

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            Logger.Debug($"ParsePackageFile():Start");
            List<Component> listComponentForBOM = new List<Component>();
            Bom bom = new Bom();
            if (DetectDeploymentType(appSettings))
            {
                isSelfContainedProject = true;
                Logger.Warn($"Deployment type identified as Self-Contained. Currently, the clearing tool does not support identification of framework packages for this deployment type solution.");
            }
            else
            {
                Logger.Debug($"Deployment type identified as Classic");
                isSelfContainedProject = false;
            }
            ParsingInputFileForBOM(appSettings, ref listComponentForBOM, ref bom);
            var componentsWithMultipleVersions = bom.Components.GroupBy(s => s.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            CheckForMultipleVersions(appSettings, componentsWithMultipleVersions);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            AddCompositionDetails(bom);
            Logger.Debug($"ParsePackageFile():End");
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
                        IsDev = isDev
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
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessNugetComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            return modifiedBOM;
        }

        private static Component ProcessNugetComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            string jfrogpackageName = $"{component.Name}.{component.Version}{ApiConstant.NugetExtension}";
            var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);

            string jfrogRepoPath = string.Empty;
            AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out jfrogRepoPath);
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData.Repo };
            Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
            Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdateNugetKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, siemensfileNameProp, jfrogRepoPathProp, hashes);

            return componentVal;
        }

        private static void UpdateNugetKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Nuget.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }
            
            if (appSettings.Nuget.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Nuget.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }
            
            if (repoValue == appSettings.Nuget.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        public static AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
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

        public static string GetJfrogRepoPath(AqlResult aqlResult)
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

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Nuget.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalNugetComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
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
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM, 
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };

                var isDirectDep = NugetDevDependencyParser.NugetDirectDependencies
                    .Any(x => x.Contains(component.Name) && x.Contains(component.Version));

                if (isDirectDep) { siemensDirect.Value = "true"; }

                component.Properties ??= new List<Property>();

                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));

                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
        }

        #endregion

        #region private methods

        private void ParsingInputFileForBOM(CommonAppSettings appSettings,
                                    ref List<Component> listComponentForBOM,
                                    ref Bom bom)
        {
            var configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Nuget);
            if (!isSelfContainedProject)
            {
                GetFrameworkPackagesForAllConfigLockFiles(configFiles);
            }

            var listOfTemplateBomfilePaths = new List<string>();
            var totalComponentsIdentified = 0;

            foreach (string filepath in configFiles)
            {
                HandleConfigFile(filepath, appSettings, ref listComponentForBOM, ref bom, listOfTemplateBomfilePaths);
            }

            PostProcessBOM(appSettings, ref listComponentForBOM, ref bom, listOfTemplateBomfilePaths, ref totalComponentsIdentified);
        }

        private void HandleConfigFile(
            string filepath,
            CommonAppSettings appSettings,
            ref List<Component> listComponentForBOM,
            ref Bom bom,
            List<string> listOfTemplateBomfilePaths)
        {
            List<Component> componentsForBOM = new List<Component>();
            if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                listOfTemplateBomfilePaths.Add(filepath);
            }

            Logger.Debug($"ParsingInputFileForBOM():FileName: {filepath}");

            if (filepath.EndsWith(FileConstant.CycloneDXFileExtension) &&
                !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                Logger.Debug($"ParsingInputFileForBOM():Found as CycloneDXFile");
                bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                CycloneDXBomParser.CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                componentsForBOM.AddRange(bom.Components);
                CommonHelper.GetDetailsForManuallyAdded(componentsForBOM, listComponentForBOM);
            }
            else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
            {                                    
                bom = _spdxBomParser.ParseSPDXBom(filepath);
                CycloneDXBomParser.CheckValidComponentsForProjectType(
                        bom.Components, appSettings.ProjectType);
                componentsForBOM.AddRange(bom.Components);
                CommonHelper.GetDetailsForManuallyAdded(componentsForBOM,
                    listComponentForBOM);
                if (bom != null)
                {
                    CommonHelper.AddSpdxSBomFileNameProperty(ref bom, filepath);
                }
                BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);

            }
            else
            {
                Logger.Debug($"ParsingInputFileForBOM():Found as Package File");
                var listofComponents = new List<NugetPackage>();
                ParseInputFiles(appSettings, filepath, listofComponents);
                var dependencies = new List<Dependency>();
                ConvertToCycloneDXModel(listComponentForBOM, listofComponents, dependencies);

                if (bom.Dependencies == null || bom.Dependencies.Count == 0)
                {
                    bom.Dependencies = dependencies;
                }
                else
                {
                    bom.Dependencies.AddRange(dependencies);
                }
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = listComponentForBOM.Count;
            }
        }

        private void PostProcessBOM(
            CommonAppSettings appSettings,
            ref List<Component> listComponentForBOM,
            ref Bom bom,
            List<string> listOfTemplateBomfilePaths,
            ref int totalComponentsIdentified)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = listComponentForBOM.Count;
            totalComponentsIdentified = listComponentForBOM.Count;

            listComponentForBOM = KeepUniqueNonDevComponents(listComponentForBOM);
            listComponentForBOM = listComponentForBOM.Distinct(new ComponentEqualityComparer()).ToList();

            if (BomCreator.bomKpiData.DuplicateComponents == 0)
            {
                BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - listComponentForBOM.Count;
            }

            BomCreator.bomKpiData.DevDependentComponents = listComponentForBOM.Count(s => s.Properties[0].Value == "true");
            bom.Components = listComponentForBOM;

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);

            if (bom != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
        }


        private static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            FileOperations fileOperations = new FileOperations();
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
            List<Dependency> subDependencies = new();
            foreach (var item in prop.Dependencies)
            {
                string purl = item;
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
            dependencies.Add(dependency);
        }

        private static List<Component> KeepUniqueNonDevComponents(List<Component> listComponentForBOM)
        {
            Dictionary<string, Component> keyValuePairs = new Dictionary<string, Component>();
            foreach (var component in listComponentForBOM)
            {
                if (!keyValuePairs.TryAdd(component.Purl, component))
                {
                    if (keyValuePairs[component.Purl].Properties[0].Value == "false" && component.Properties[0].Value == "true")
                    {
                        //Already Comp with Development Dependent added as 'false' ,remove that Comp
                        //& add New Comp as Development Dependent only if 'true'
                        keyValuePairs.Remove(component.Purl);
                        keyValuePairs.Add(component.Purl, component);
                    }
                }
            }
            BomCreator.bomKpiData.DuplicateComponents = listComponentForBOM.Count - keyValuePairs.Values.Count;
            return keyValuePairs.Values.ToList();
        }

        private void ParseInputFiles(CommonAppSettings appSettings, string filepath, List<NugetPackage> listofComponents)
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
                    NugetDevDependencyParser.AddRangeDirectDependencies(list.Select(x => x.ID + " " + x.Version));
                }

                listofComponents.AddRange(list);
            }
            else
            {
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
                var packageDetails = PackageDetailsRegex().Match(library).Groups[1].Value;

                Match m = PackageDetailsMatchRegex().Match(packageDetails);
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

        private List<NugetPackage> ParseAssetFile(string configFile)
        {
            NugetDevDependencyParser nugetDevDependencyParser = NugetDevDependencyParser.Instance;
            List<Container> containers = nugetDevDependencyParser.Parse(configFile);
            return ConvertContainerAsNugetPackage(containers, configFile);
        }

        private List<NugetPackage> ConvertContainerAsNugetPackage(List<Container> containers, string configFile)
        {
            List<NugetPackage> nugetPackages = new List<NugetPackage>();
            List<string> uniqueFrameworkKeys = GetUniqueTargetFrameworkKeysForConfigFile(configFile);
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
                        IsDev = (!isSelfContainedProject && IsFrameworkDependentComponent(lst.Value.Name, lst.Value.Version, uniqueFrameworkKeys)) || lst.Value.Scope.ToString() == "DevDependency" ? "true" : "false",
                    });
                }
            }
            return nugetPackages;
        }

        private static void GetDependencyList(KeyValuePair<string, BuildInfoComponent> lst, ref List<string> depvalue)
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

        private bool IsFrameworkDependentComponent(string name, string version, List<string> uniqueFrameworkKeys)
        {
            if (IsAlreadyAddedToInputFilesList(name, version, uniqueFrameworkKeys))
            {
                return true;
            }

            bool isFrameworkDependent = IsInFrameworkPackages(name, version, uniqueFrameworkKeys);

            if (isFrameworkDependent)
            {
                AddToFrameworkPackagesInInputFiles(name, version, uniqueFrameworkKeys);
            }

            return isFrameworkDependent;
        }

        private bool IsAlreadyAddedToInputFilesList(string name, string version, List<string> uniqueFrameworkKeys)
        {
            foreach (var key in uniqueFrameworkKeys)
            {
                string runtime = key.Split('-')[0];
                if (_listofFrameworkPackagesInInputFiles.TryGetValue(runtime, out var packages))
                {
                    if (packages.TryGetValue(name, out var pkgVersion) && pkgVersion.ToNormalizedString() == version)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsInFrameworkPackages(string name, string version, List<string> uniqueFrameworkKeys)
        {
            foreach (var key in uniqueFrameworkKeys)
            {
                if (_listofFrameworkPackages.TryGetValue(key, out var packages))
                {
                    if (packages.TryGetValue(name, out var pkgVersion) && pkgVersion.ToNormalizedString() == version)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddToFrameworkPackagesInInputFiles(string name, string version, List<string> uniqueFrameworkKeys)
        {
            foreach (var frameworkKey in uniqueFrameworkKeys)
            {
                string runtime = frameworkKey.Split('-')[0];
                if (_listofFrameworkPackages.TryGetValue(frameworkKey, out var packages) &&
                    packages.TryGetValue(name, out var pkgVersion) &&
                    pkgVersion.ToNormalizedString() == version)
                {
                    if (!_listofFrameworkPackagesInInputFiles.TryGetValue(runtime, out Dictionary<string, NuGetVersion> value))
                    {
                        value = new Dictionary<string, NuGetVersion>();
                        _listofFrameworkPackagesInInputFiles[runtime] = value;
                    }

                    if (!value.ContainsKey(name))
                    {
                        value[name] = NuGetVersion.Parse(version);
                        Logger.Debug($"Framework dependent component added: {name} {version} for target framework: {frameworkKey}");
                    }
                }
            }
        }

        private void GetFrameworkPackagesForAllConfigLockFiles(List<string> configFiles)
        {
            try
            {
                List<string> projectAssetsFiles = configFiles.Where(file => file.EndsWith(FileConstant.NugetAssetFile) && File.Exists(file)).ToList();
                _listofFrameworkPackages = _frameworkPackages.GetFrameworkPackages(projectAssetsFiles);
            }
            catch (ArgumentNullException ex)
            {
                Logger.Debug($"Error in GetFrameworkPackagesForAllConfigLockFiles:", ex);
            }
        }

        private List<string> GetUniqueTargetFrameworkKeysForConfigFile(string configFile)
        {
            List<string> uniqueKeys = [];
            try
            {
                var lockFile = new LockFileFormat().Read(configFile);

                foreach (var target in lockFile.Targets)
                {
                    var targetFramework = target.TargetFramework;
                    var frameworkReferences = _frameworkPackages.GetFrameworkReferences(lockFile, target);
                    foreach (var framework in frameworkReferences)
                    {
                        if (!uniqueKeys.Contains(targetFramework + "-" + framework))
                        {
                            uniqueKeys.Add(targetFramework + "-" + framework);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.Debug($"GetUniqueTargetFrameworkKeysForConfigFile: IO error while reading the config file '{configFile}':", ex);
            }
            catch (ArgumentNullException ex)
            {
                Logger.Debug($"Error in GetUniqueTargetFrameworkKeysForConfigFile:", ex);
            }
            return uniqueKeys;
        }

        private static bool DetectDeploymentType(CommonAppSettings appSettings)
        {
            string[] projectFiles = FileConstant.Nuget_DeploymentType_DetectionExt
                .SelectMany(ext => Directory.GetFiles(appSettings.Directory.InputFolder, ext, SearchOption.AllDirectories))
                .Where(file => !IsExcluded(file, appSettings.Nuget?.Exclude))
                .ToArray();

            bool isSelfContained = false;
            bool isSingleFile = false;

            foreach (var projectFilePath in projectFiles)
            {
                try
                {
                    (bool selfContained, bool singleFile) = CheckDeploymentTags(projectFilePath);
                    isSelfContained |= selfContained;
                    isSingleFile |= singleFile;
                    if (isSelfContained || isSingleFile)
                    {
                        // Early exit if either is detected
                        break;
                    }
                }
                catch (IOException ex)
                {
                    Logger.Debug($"IO error while Loading project file path :{projectFilePath} Error : {ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    Logger.Debug($"IO error while Loading project file path :{projectFilePath} Error : {ex.Message}");
                }
            }

            return isSelfContained || isSingleFile;
        }

        private static (bool isSelfContained, bool isSingleFile) CheckDeploymentTags(string projectFilePath)
        {
            bool isSelfContained = false;
            bool isSingleFile = false;

            XDocument projectDocument = XDocument.Load(projectFilePath);
            foreach (var tag in FileConstant.Nuget_DeploymentType_DetectionTags)
            {
                var element = projectDocument.Descendants(tag).FirstOrDefault();
                if (element != null)
                {
                    bool result;
                    switch (tag)
                    {
                        case "SelfContained":
                            isSelfContained = bool.TryParse(element.Value, out result) && result;
                            break;
                        case "PublishSingleFile":
                            isSingleFile = bool.TryParse(element.Value, out result) && result;
                            break;
                    }
                }
            }
            return (isSelfContained, isSingleFile);
        }

        private static bool IsExcluded(string filePath, string[] excludePatterns)
        {
            if (excludePatterns == null || excludePatterns.Length == 0)
            {
                return false;
            }

            return excludePatterns.Any(pattern => filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private void AddCompositionDetails(Bom bom)
        {
            int totalCount = 0;
            // Remove duplicates in _listofFrameworkPackagesInInputFiles
            foreach (var frameworkKey in _listofFrameworkPackagesInInputFiles.Keys.ToList())
            {
                var uniquePackages = _listofFrameworkPackagesInInputFiles[frameworkKey]
                    .GroupBy(package => package.Key)
                    .ToDictionary(group => group.Key, group => group.First().Value);

                if (uniquePackages.Count != _listofFrameworkPackagesInInputFiles[frameworkKey].Count)
                {
                    Logger.Debug($"AddCompositionDetails: Removed {_listofFrameworkPackagesInInputFiles[frameworkKey].Count - uniquePackages.Count} duplicate packages for framework '{frameworkKey}'.");
                }

                _listofFrameworkPackagesInInputFiles[frameworkKey] = uniquePackages;
                totalCount = totalCount + uniquePackages.Count;
            }

            Logger.Warn($"Total Framework packages marked as development dependencies: {totalCount}");
            // Add compositions to the BOM
            _compositionBuilder.AddCompositionsToBom(bom, _listofFrameworkPackagesInInputFiles);
        }
        [GeneratedRegex(@"packages\\(.+?)\\lib")]
        private static partial Regex PackageDetailsRegex();
        [GeneratedRegex(@"\d+")]
        private static partial Regex PackageDetailsMatchRegex();
        #endregion
    }
}