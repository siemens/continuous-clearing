﻿
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
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using log4net.Core;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using System.Xml.Linq;
using Component = CycloneDX.Models.Component;
using static System.Net.Mime.MediaTypeNames;
using LCT.Services.Interface;
using LCT.APICommunications.Model.AQL;

namespace LCT.PackageIdentifier
{
    public class PythonProcessor : IParser
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public async Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {
            List<Component> componentNotForBOM = new List<Component>();

            // Python Artifactory checking code..
            return await Task.FromResult(componentNotForBOM);
        }

        public async Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo)
        {
            List<Component> componentForBOM = new List<Component>();

            // Python Artifactory checking code..
            return await Task.FromResult(componentForBOM);
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

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings)
        {
            List<Component> Internalcomponents;
            if (appSettings.InternalRepoList != null && appSettings.InternalRepoList.Length > 0)
            {
                Internalcomponents = await ComponentIdentification(componentData.comparisonBOMData, appSettings);
                foreach (var item in Internalcomponents)
                {
                    Component component = componentData.comparisonBOMData.First(x => x.Name == item.Name && x.Version == item.Version);
                    Property internalType = new()
                    {
                        Name = Dataconstant.Cdx_IsInternal,
                        Value = "true"
                    };
                    component.Properties.Add(internalType);

                }
                componentData.internalComponents = Internalcomponents;
                BomCreator.bomKpiData.InternalComponents = Internalcomponents.Count;
            }

            return componentData;
        }

        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.PackageFilePath, appSettings.Python);
            List<PythonPackage> listofComponents = new List<PythonPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;
            bool mainLockFileScanned = false;

            foreach (string config in configFiles)
            {
                if (config.EndsWith("poetry.lock") && !mainLockFileScanned)
                {
                    listofComponents.AddRange(ExtractDetailsForPoetryLockfile(config));
                    mainLockFileScanned = true;
                }
                else if (config.EndsWith(FileConstant.CycloneDXFileExtension))
                {
                    listofComponents.AddRange(ExtractDetailsFromJson(config));
                }
            }

            int initialCount = listofComponents.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = listComponentForBOM.Count;
            bom.Components = listComponentForBOM;
            bom = RemoveExcludedComponents(appSettings, bom);
            return bom;
        }

        #region Private Methods

        private static List<PythonPackage> ExtractDetailsForPoetryLockfile(string filePath)
        {
            List<PythonPackage> PythonPackages;
            PythonPackages = PoetrySetOfCmds(filePath);
            return PythonPackages;
        }

        private static List<PythonPackage> ExtractDetailsFromJson(string filePath)
        {
            List<PythonPackage> PythonPackages = new List<PythonPackage>();
            Model.CycloneDxBomData cycloneDxBomData;
            string json = File.ReadAllText(filePath);
            cycloneDxBomData = JsonConvert.DeserializeObject<CycloneDxBomData>(json);

            if (cycloneDxBomData != null && cycloneDxBomData.ComponentsInfo != null)
            {
                foreach (var componentsInfo in cycloneDxBomData.ComponentsInfo)
                {
                    if (componentsInfo.Type == "library")
                    {
                        BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                        PythonPackage package = new PythonPackage
                        {
                            Name = componentsInfo.Name,
                            Version = componentsInfo.Version,
                            PurlID = componentsInfo.ReleaseExternalId,
                        };

                        if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.ReleaseExternalId) && componentsInfo.ReleaseExternalId.Contains(Dataconstant.PythonPackage))
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
                }
            }
            else
            {
                Logger.Debug($"ExtractDetailsFromJson():NoComponenstFound!!");
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

            return $"{Dataconstant.PythonPackage}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
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
                        Name = Dataconstant.Cdx_IsDevelopmentDependency,
                        Value = "TRUE"
                    };
                }
                else
                {
                    devDependency = new()
                    {
                        Name = Dataconstant.Cdx_IsDevelopmentDependency,
                        Value = "FALSE"
                    };
                }

                Component component = new Component
                {
                    Name = prop.Name,
                    Version = prop.Version,
                    Purl = GetReleaseExternalId(prop.Name, prop.Version),
                };

                component.Properties = new List<Property>
                {
                    devDependency
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

        private async Task<List<Component>> ComponentIdentification(List<Component> comparisonBOMData, CommonAppSettings appSettings)
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

        private async Task<List<Component>> CheckPackageAvailability(CommonAppSettings appSettings, Component component, string repo)
        {

            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey

            };
            List<Component> componentNotForBOM = await CheckInternalComponentsInJfrogArtifactory(appSettings, artifactoryUpload, component, repo);
            return componentNotForBOM;

        }

        private async Task<List<Component>> AddPackageAvailability(CommonAppSettings appSettings, Component component)
        {
            List<Component> modifiedBOM = new List<Component>();
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey
            };

            var repolist = appSettings?.Python?.JfrogPythonRepoList;
            if (repolist != null)
            {
                foreach (var item in repolist)
                {
                    List<Component> componentsForBOM = await GetJfrogArtifactoryRepoInfo(appSettings, artifactoryUpload, component, item);
                    if (componentsForBOM.Count > 0)
                    {
                        modifiedBOM = componentsForBOM;
                        break;
                    }
                }
            }
            else
            {
                Logger.Debug($"AddPackageAvailability():No Repo list manitained!!");
            }

            return modifiedBOM;
        }

        private static List<PythonPackage> ExecutePoetryCMD(string CommandForPoetry)
        {
            List<PythonPackage> packages = new List<PythonPackage>();
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
                    Logger.Debug($"GetHashCodeUsingNpmView():Linux OS Found!!");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                    p.StartInfo.Arguments = "/c " + CommandForPoetry;
                    Logger.Debug($"GetHashCodeUsingNpmView():Windows OS Found!!");
                }
                else
                {
                    Logger.Debug($"GetHashCodeUsingNpmView():OS Details not Found!!");
                }

                // Run as administrator
                p.StartInfo.Verb = "runas";

                var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo, timeoutInMs);
                result = processResult?.Result;
            }
            if (result != null && result.ExitCode == 0)
            {
                var strings = result.StdOut.Split(Environment.NewLine).ToList();

                foreach (var package in strings)
                {
                    var lst = package.Split(" ");
                    lst = lst.Where(x => !string.IsNullOrEmpty(x)).Where(y => !y.Contains("(!)")).ToArray();

                    if (lst.Length > 1)
                    {
                        packages.Add(new PythonPackage()
                        {
                            Name = lst[0],
                            Version = lst[1]
                        });
                    }
                }
            }
            else
            {
                Logger.Debug($"ExecutePoetryCMD():Poetry CMD execution failed : " + result?.StdErr);
            }

            return packages;
        }

        private static List<PythonPackage> PoetrySetOfCmds(string SourceFilePath)
        {
            List<PythonPackage> lst = new List<PythonPackage>();
            string CommandForALlComp = "poetry show -C " + SourceFilePath;
            string CommandForMainComp = "poetry show --only main -C " + SourceFilePath;

            List<PythonPackage> AllComps = ExecutePoetryCMD(CommandForALlComp);
            List<PythonPackage> MainComps = ExecutePoetryCMD(CommandForMainComp);

            foreach (var val in AllComps)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                if (MainComps.Any(a => a.Name == val.Name && a.Version == val.Version))
                {
                    val.Isdevdependent = false;
                }
                else
                {
                    val.Isdevdependent = true;
                    BomCreator.bomKpiData.DevDependentComponents++;
                }
                lst.Add(val);
            }

            return lst;
        }

        public Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            return Task.FromResult(componentData);
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Python?.JfrogNpmRepoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                //string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper);
                string repoName = NotFoundInRepo;
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

        #endregion
    }
}