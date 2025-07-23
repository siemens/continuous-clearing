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
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Tommy;
using Component = CycloneDX.Models.Component;
using Dependency = CycloneDX.Models.Dependency;

namespace LCT.PackageIdentifier
{
    public class PythonProcessor(ICycloneDXBomParser cycloneDXBomParser,ISpdxBomParser spdxBomParser) : IParser
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };

        public Bom ParsePackageFile(CommonAppSettings appSettings,ref Bom unSupportedBomList)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Poetry);
            List<PythonPackage> listofComponents = new List<PythonPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;
            List<Dependency> dependencies = new List<Dependency>();           
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string config in configFiles)
            {
                if (config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(config);
                }
                if (config.ToLower().EndsWith("poetry.lock"))
                {
                    listofComponents.AddRange(ExtractDetailsForPoetryLockfile(config, dependencies));
                }
                else if ((config.EndsWith(FileConstant.CycloneDXFileExtension) || config.EndsWith(FileConstant.SPDXFileExtension))
         && !config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listofComponents.AddRange(ExtractDetailsFromJson(config, appSettings, ref dependencies));
                }
            }


            int initialCount = listofComponents.Count;
            int totalUnsupportedComponents = ListUnsupportedComponentsForBom.Components.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = listComponentForBOM.Count;
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponents - ListUnsupportedComponentsForBom.Components.Count;
            bom.Components = listComponentForBOM;
            bom.Dependencies = dependencies;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);
            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();

            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            } 
            SpdxSbomHelper.AddDevelopmentProperty(ListUnsupportedComponentsForBom.Components);
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }

        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            List<string> pythonDirectDependencies = new List<string>();
            pythonDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };
                if (pythonDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version)))
                {
                    siemensDirect.Value = "true";
                }

                component.Properties ??= new List<Property>();
                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));

                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
        }

        #region Private Methods

        public static List<PythonPackage> ExtractDetailsForPoetryLockfile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages;
            PythonPackages = GetPackagesFromTOMLFile(filePath, dependencies);
            return PythonPackages;
        }

        private static List<PythonPackage> GetPackagesFromTOMLFile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages = new();
            List<KeyValuePair<string, TomlNode>> keyValuePair = new();
            FileParser fileParser = new();
            TomlTable tomlTable = fileParser.ParseTomlFile(filePath);

            foreach (TomlNode node in tomlTable["package"])
            {
                PythonPackage pythonPackage = new()
                {
                    Name = node["name"].ToString(),
                    Version = node["version"].ToString(),
                    PurlID = Dataconstant.PurlCheck()["POETRY"] + "/" + node["name"].ToString() + "@" + node["version"].ToString(),
                    // By Default Tommy.TomlLazy is coming instead of Null or empty
                    Isdevdependent = (node["category"].ToString() != "main" && node["category"].ToString() != "Tommy.TomlLazy"),
                    FoundType = Dataconstant.Discovered,
                    SpdxComponentDetails=new SpdxComponentInfo()
                };

                if (pythonPackage.Isdevdependent)
                    BomCreator.bomKpiData.DevDependentComponents++;

                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                PythonPackages.Add(pythonPackage);
                keyValuePair.Add(new KeyValuePair<string, TomlNode>(pythonPackage.PurlID, node));
            }

            GetRefDetailsFromDependencyText(keyValuePair, dependencies, PythonPackages);

            return PythonPackages;
        }

        private static void GetRefDetailsFromDependencyText(List<KeyValuePair<string, TomlNode>> keyValues, List<Dependency> dependencies, List<PythonPackage> PythonPackages)
        {
            foreach (var node in keyValues)
            {
                var dep = node.Value["dependencies"];
                List<Dependency> subDependencies = new();
                if (dep != null && dep.ChildrenCount > 0)
                {
                    foreach (var dependency in dep.AsTable.RawTable)
                    {
                        subDependencies.Add(new Dependency()
                        {
                            Ref = FormRefFromNodeDetails(dependency, PythonPackages)
                        });
                    }
                }

                dependencies.Add(new Dependency()
                {
                    Ref = node.Key,
                    Dependencies = subDependencies
                });
            }
        }

        private static string FormRefFromNodeDetails(KeyValuePair<string, TomlNode> valuePair, List<PythonPackage> PythonPackages)
        {
            var value = PythonPackages.Find(val => val.Name == valuePair.Key)?.Version;

            if (string.IsNullOrEmpty(value))
            {
                return Dataconstant.PurlCheck()["POETRY"] + "/" + valuePair.Key + "@" + "*";
            }
            else
            {
                return Dataconstant.PurlCheck()["POETRY"] + "/" + valuePair.Key + "@" + value;
            }
        }

        private List<PythonPackage> ExtractDetailsFromJson(string filePath, CommonAppSettings appSettings, ref List<Dependency> dependencies)
        {
            Bom bom;
            List<PythonPackage> PythonPackages = new List<PythonPackage>();
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                bom = _spdxBomParser.ParseSPDXBom(filePath);
                SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType,ref listUnsupportedComponents);
                SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filePath);
                ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
            }
            else
            {
                bom = _cycloneDXBomParser.ParseCycloneDXBom(filePath);
                CycloneDXBomParser.CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
            }            

            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                PythonPackage package = new PythonPackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                    SpdxComponentDetails = new SpdxComponentInfo(),
                };
                SetSpdxComponentDetails(filePath, package);

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["POETRY"]))
                {
                    BomCreator.bomKpiData.DebianComponents++;
                    PythonPackages.Add(package);
                    Logger.Debug($"ExtractDetailsFromJson():ValidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"ExtractDetailsFromJson():InvalidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
            }

            if (bom.Dependencies != null)
            {
                dependencies.AddRange(bom.Dependencies);
            }

            return PythonPackages;
        }

        private static void GetDistinctComponentList(ref List<PythonPackage> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.PurlID }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        private static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["POETRY"]}{Dataconstant.ForwardSlash}{name}@{version}";
        }

        private static List<Component> FormComponentReleaseExternalID(List<PythonPackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                string releaseExternalId = GetReleaseExternalId(prop.Name, prop.Version);
                Component component = CommonHelper.CreateComponentWithProperties(
                    prop.Name,
                    prop.Version,
                    releaseExternalId
                );
                AddComponentProperties(prop, component);
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        private static Bom RemoveExcludedComponents(CommonAppSettings appSettings,
            Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM, 
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetPypiListOfComponentsFromRepo(appSettings.Poetry.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalPythonComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;

            return componentData;
        }

        private static bool IsInternalPythonComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = bomHelper.GetFullNameOfComponent(component);
            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == jfrogcomponentName) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version)))
            {
                return true;
            }

            return false;
        }


        private static string GetJfrogNameOfPypiComponent(string name, string version, List<AqlResult> aqlResultList)
        {


            string nameVerison = string.Empty;
            nameVerison = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == name) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == version))?.Name ?? string.Empty;

            if (string.IsNullOrEmpty(nameVerison)) { nameVerison = Dataconstant.PackageNameNotFoundInJfrog; }
            return nameVerison;
        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetPypiListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessPythonComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            return modifiedBOM;
        }

        private static Component ProcessPythonComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper, out string jfrogPackageNameWhlExten, out string jfrogRepoPath);

            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == component.Name) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version));

            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property fileNameProperty = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = jfrogPackageNameWhlExten };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdatePythonKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, fileNameProperty, jfrogRepoPathProperty, hashes);

            return componentVal;
        }

        private static void UpdatePythonKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Poetry.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }
            
            if (appSettings.Poetry.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Poetry.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }
            
            if (repoValue == appSettings.Poetry.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList,
                                                     Component component,
                                                     IBomHelper bomHelper,
                                                     out string jfrogPackageName,
                                                     out string jfrogRepoPath)
        {
            jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogPackageNameWhlExten = GetJfrogNameOfPypiComponent(
                component.Name, component.Version, aqlResultList);

            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase));
            jfrogPackageName = jfrogPackageNameWhlExten;

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = GetJfrogNameOfPypiComponent(fullName, component.Version, aqlResultList);
                if (!fullNameVersion.Equals(jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase))
                {
                    var aqllist = aqlResultList.FindAll(x => x.Name.Contains(fullNameVersion, StringComparison.OrdinalIgnoreCase)
                    && (x.Name.EndsWith(ApiConstant.PythonExtension) || x.Name.EndsWith(FileConstant.TargzFileExtension)));
                    jfrogPackageName = fullNameVersion;
                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                var aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }

            if (string.IsNullOrEmpty(jfrogPackageName))
            {
                jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            }

            return repoName;
        }

        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }
        private static void AddComponentProperties(PythonPackage prop, Component component)
        {
            var devDependency = new Property
            {
                Name = Dataconstant.Cdx_IsDevelopment,
                Value = prop.Isdevdependent ? "true" : "false"
            };

            component.Properties ??= new List<Property>();

            if (prop.SpdxComponentDetails.SpdxComponent)
            {
                AddSpdxProperties(prop, component);
                component.Properties.Add(devDependency);
            }
            else
            {
                AddIdentifierTypeProperty(prop, component, devDependency);
            }
        }

        private static void AddSpdxProperties(PythonPackage prop, Component component)
        {
            string fileName = Path.GetFileName(prop.SpdxComponentDetails.SpdxFilePath);
            SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
        }

        private static void AddIdentifierTypeProperty(PythonPackage prop, Component component, Property devDependency)
        {
            var identifierType = new Property
            {
                Name = Dataconstant.Cdx_IdentifierType,
                Value = prop.FoundType == Dataconstant.Discovered ? Dataconstant.Discovered : Dataconstant.ManullayAdded
            };

            component.Properties.Add(devDependency);
            component.Properties.Add(identifierType);
        }
        private static void SetSpdxComponentDetails(string filePath, PythonPackage package)
        {
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                package.SpdxComponentDetails.SpdxFilePath = filePath;
                package.SpdxComponentDetails.SpdxComponent = true;
            }
        }
        #endregion
    }
}
