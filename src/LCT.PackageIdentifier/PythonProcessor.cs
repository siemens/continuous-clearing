// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Component = CycloneDX.Models.Component;

namespace LCT.PackageIdentifier
{
    [ExcludeFromCodeCoverage]
    public class PythonProcessor : IParser
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly CycloneDXBomParser cycloneDXBomParser;

        public PythonProcessor()
        {
            cycloneDXBomParser = new CycloneDXBomParser();
        }

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Python);
            List<PythonPackage> listofComponents = new List<PythonPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;
            List<Dependency> dependencies = new List<Dependency>();

            foreach (string config in configFiles)
            {
                if (config.EndsWith("poetry.lock"))
                {
                    listofComponents.AddRange(ExtractDetailsForPoetryLockfile(config, dependencies));
                }
                else if (config.EndsWith(FileConstant.CycloneDXFileExtension) && !config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listofComponents.AddRange(ExtractDetailsFromJson(config, appSettings, ref dependencies));
                }
            }

            Bom templateDetails = new Bom();
            if (File.Exists(appSettings.CycloneDxSBomTemplatePath) && appSettings.CycloneDxSBomTemplatePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                templateDetails = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(cycloneDXBomParser.ParseCycloneDXBom(appSettings.CycloneDxSBomTemplatePath));
                CycloneDXBomParser.CheckValidComponentsForProjectType(templateDetails.Components, appSettings.ProjectType);
            }

            int initialCount = listofComponents.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = listComponentForBOM.Count;

            bom.Components = listComponentForBOM;
            bom.Dependencies = dependencies;
            //Adding Template Component Details & MetaData
            SbomTemplate.AddComponentDetails(bom.Components, templateDetails);
            bom = RemoveExcludedComponents(appSettings, bom);
            return bom;
        }

        #region Private Methods

        public static List<PythonPackage> ExtractDetailsForPoetryLockfile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages;
            PythonPackages = PoetrySetOfCmds(filePath, dependencies);
            return PythonPackages;
        }
        
        private List<PythonPackage> ExtractDetailsFromJson(string filePath, CommonAppSettings appSettings, ref List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages = new List<PythonPackage>();
            Bom bom = cycloneDXBomParser.ParseCycloneDXBom(filePath);
            CycloneDXBomParser.CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);

            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                PythonPackage package = new PythonPackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                };

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["PYTHON"]))
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

            return $"{Dataconstant.PurlCheck()["PYTHON"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static List<Component> FormComponentReleaseExternalID(List<PythonPackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();
            Property devDependency;

            foreach (var prop in listOfComponents)
            {
                if (prop.Isdevdependent)
                {
                    devDependency = new()
                    {
                        Name = Dataconstant.Cdx_IsDevelopment,
                        Value = "true"
                    };
                }
                else
                {
                    devDependency = new()
                    {
                        Name = Dataconstant.Cdx_IsDevelopment,
                        Value = "false"
                    };
                }

                Component component = new Component
                {
                    Name = prop.Name,
                    Version = prop.Version,
                    Purl = GetReleaseExternalId(prop.Name, prop.Version),
                };

                Property identifierType;
                if (prop.FoundType == Dataconstant.Discovered)
                {
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                }
                else
                {
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                }

                component.Properties = new List<Property>
                {
                    devDependency,
                    identifierType
                };

                component.BomRef = component.Purl;

                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        private static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            int noOfExcludedComponents = 0;
            if (appSettings.Python.ExcludedComponents != null)
            {
                componentForBOM = CommonHelper.RemoveExcludedComponents(componentForBOM, appSettings.Python.ExcludedComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcluded += noOfExcludedComponents;

            }
            cycloneDXBOM.Components = componentForBOM;
            return cycloneDXBOM;
        }

        private static Result ExecutePoetryCMD(string CommandForPoetry)
        {
            Result result;
            const int timeoutInMs = 200 * 60 * 1000;
            using (Process p = new Process())
            {
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    p.StartInfo.FileName = Path.Combine(@"/bin/bash");
                    p.StartInfo.Arguments = "-c \" " + CommandForPoetry + " \"";
                    Logger.Debug($"ExecutePoetryCMD():Linux OS Found!!");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                    p.StartInfo.Arguments = "/c " + CommandForPoetry;
                    Logger.Debug($"ExecutePoetryCMD():Windows OS Found!!");
                }
                else
                {
                    Logger.Debug($"ExecutePoetryCMD():OS Details not Found!!");
                }

                // Run as administrator
                p.StartInfo.Verb = "runas";

                var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo, timeoutInMs);
                result = processResult?.Result;
            }
            if (result != null && result.ExitCode == 0)
            {
                Logger.Debug($"ExecutePoetryCMD():Poetry CMD execution Success : " + result?.StdOut);
            }
            else
            {
                Logger.Debug($"ExecutePoetryCMD():Poetry CMD execution failed : " + result?.StdErr);
            }

            return result;
        }

        private static List<PythonPackage> GetPackagesFromPoetryOutput(Result result)
        {
            List<PythonPackage> packages = new List<PythonPackage>();
            var strings = result.StdOut.Split(Environment.NewLine).ToList();

            foreach (var package in strings)
            {
                //Needs to extract Name & Version details from EX: "attrs (!) 22.2.0 Classes Without Boilerplate"
                var lst = package.Split(" ");
                lst = lst.Where(x => !string.IsNullOrEmpty(x)).Where(y => !y.Contains("(!)")).ToArray();

                if (lst.Length > 1)
                {
                    packages.Add(new PythonPackage()
                    {
                        Name = lst[0],
                        Version = lst[1],
                        PurlID = "pkg:pypi/" + lst[0] + "@" + lst[1] + "?arch=source"
                    });
                }
            }
            return packages;
        }

        private static List<PythonPackage> PoetrySetOfCmds(string SourceFilePath, List<Dependency> dependencies)
        {

            List<PythonPackage> lst = new List<PythonPackage>();
            string CommandForALlComp = "poetry show -C " + SourceFilePath;
            string CommandForMainComp = "poetry show --only main -C " + SourceFilePath;
            const string showCMD = "poetry show ";
            List<PythonPackage> AllComps = GetPackagesFromPoetryOutput(ExecutePoetryCMD(CommandForALlComp));
            List<PythonPackage> MainComps = GetPackagesFromPoetryOutput(ExecutePoetryCMD(CommandForMainComp));

            foreach (var val in AllComps)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                if (MainComps.Exists(a => a.Name == val.Name && a.Version == val.Version))
                {
                    val.Isdevdependent = false;
                }
                else
                {
                    val.Isdevdependent = true;
                    BomCreator.bomKpiData.DevDependentComponents++;
                }

                //Adding dependencies
                Result result = ExecutePoetryCMD(showCMD + val.Name + " -C " + SourceFilePath);
                Dependency dependency = GetDependenciesDetails(result, val, AllComps);
                if (dependency.Dependencies != null)
                {
                    dependencies.Add(dependency);
                }

                val.FoundType = Dataconstant.Discovered;
                lst.Add(val);
            }

            return lst;
        }

        private static Dependency GetDependenciesDetails(Result result, PythonPackage mainComp, List<PythonPackage> AllComps)
        {
            Dependency dependency = new Dependency();

            if (result != null && result.StdOut.Contains("dependencies"))
            {
                var details = result.StdOut;
                List<string> lines = details.Split(Environment.NewLine).ToList();
                bool addDependencies = false;
                List<string> dependencyList = new List<string>();

                foreach (string line in lines)
                {
                    if (line == "dependencies")
                    {
                        addDependencies = true;
                        continue;
                    }

                    if (addDependencies && !string.IsNullOrEmpty(line))
                    {
                        string comp = line;
                        comp = comp.Replace(" - ", "");
                        dependencyList.Add(comp.Split(" ")[0]);
                    }

                    if (string.IsNullOrEmpty(line))
                        addDependencies = false;
                }
                dependency = GetDependencyMappings(mainComp, dependencyList, AllComps);
            }
            return dependency;
        }

        private static Dependency GetDependencyMappings(PythonPackage mainComp, List<string> dependencyList, List<PythonPackage> AllComps)
        {
            List<Dependency> subDependencies = new();
            foreach (var item in dependencyList)
            {
                try
                {
                    var purl = AllComps.Find(comp => comp.Name == item)?.PurlID;
                    if (!string.IsNullOrEmpty(purl))
                    {
                        Dependency dependentList = new Dependency()
                        {
                            Ref = purl
                        };
                        subDependencies.Add(dependentList);
                    }
                    else
                    {
                        //Adding just NAME as subdependencies insted fo PURLID
                        Dependency dependentList = new Dependency()
                        {
                            Ref = item + " *"
                        };
                        subDependencies.Add(dependentList);
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Logger.Error($"GetDependencyMappings(): " + mainComp.Name, ex);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error($"GetDependencyMappings(): " + mainComp.Name, ex);
                }
            }
            var dependency = new Dependency()
            {
                Ref = mainComp.PurlID,
                Dependencies = subDependencies
            };

            return dependency;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.InternalRepoList, jFrogService);

            // find the components in the list of internal components
            List<Component> internalComponents = new List<Component>();
            var internalComponentStatusUpdatedList = new List<Component>();
            var inputIterationList = componentData.comparisonBOMData;

            foreach (Component component in inputIterationList)
            {
                var currentIterationItem = component;
                bool isTrue = IsInternalPythonComponent(aqlResultList, currentIterationItem, bomhelper);
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

        private static bool IsInternalPythonComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}.tar.gz";
            if (aqlResultList.Exists(x => x.Name.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)
                && aqlResultList.Exists(
                x => x.Name.Equals(fullNameVersion, StringComparison.OrdinalIgnoreCase) && (x.Name.EndsWith(".whl") || x.Name.EndsWith(".tar.gz"))))
            {
                return true;
            }
            return false;
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Python?.JfrogPythonRepoList, jFrogService);
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
            string jfrogcomponentName = $"{component.Name}-{component.Version}.tar.gz";

            string repoName = aqlResultList.Find(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase))?.Repo ?? NotFoundInRepo;

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";

            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) &&
                repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                repoName = aqlResultList.Find(x => x.Name.Contains(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase) && (x.Name.EndsWith(".whl") || x.Name.EndsWith(".tar.gz")))?.Repo ?? NotFoundInRepo;
            }

            return repoName;
        }

        #endregion
    }
}
