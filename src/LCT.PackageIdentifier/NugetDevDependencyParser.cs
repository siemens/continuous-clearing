// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.PackageIdentifier.Model.NugetModel;
using log4net;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Locator;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Directory = System.IO.Directory;

namespace LCT.PackageIdentifier
{
    public class NugetDevDependencyParser
    {
        private static NugetDevDependencyParser instance = null;
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static List<string> NugetDirectDependencies = new List<string>();

        private NugetDevDependencyParser()
        {
            MSBuildLocator.RegisterDefaults();
        }

        public static NugetDevDependencyParser Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NugetDevDependencyParser();
                }
                return instance;
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public List<Container> Parse(string configFile)
#pragma warning restore CA1822 // Mark members as static
        {
            List<Container> containerList = new();

            Container container = new()
            {
                Components = new Dictionary<string, BuildInfoComponent>(),
                Name = Path.GetFileName(configFile),
                Type = ContainerType.nuget
            };
            containerList.Add(container);

            ParseJsonFile(configFile, container);

            return containerList;
        }

        private static bool IsDevDependecy(LockFileTargetLibrary library)
        {
            return library.CompileTimeAssemblies.Count == 0
                && library.ContentFiles.Count == 0
                && library.EmbedAssemblies.Count == 0
                && library.FrameworkAssemblies.Count == 0
                && library.NativeLibraries.Count == 0
                && library.ResourceAssemblies.Count == 0
                && library.ToolsAssemblies.Count == 0;
        }

        private static bool IsTestProject(string projectPath)
        {
            Project csProj;
            try
            {
                csProj = new Project(projectPath);
            }
            catch (InvalidProjectFileException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Evaluate Test Project", "IsTestProject()", ex, $"Failed to read project file: {projectPath}");
                Logger.Warn($"Failed to read project file, evaluation fails for : " + projectPath);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Evaluate Test Project", "IsTestProject()", ex, $"Failed to read project file: {projectPath}");
                Logger.Warn($"Failed to read project file, Maybe there is already an equivalent project loaded in the project collection " + projectPath);
                return false;
            }
            catch (MissingFieldException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Evaluate Test Project", "IsTestProject()", ex, $"Failed to read project file: {projectPath}");
                Logger.Warn($"Unable to read project file : " + projectPath);
                return false;
            }
            catch (ArgumentException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Evaluate Test Project", "IsTestProject()", ex, $"Failed to read project file: {projectPath}");
                Logger.Warn($"Unable to read project file : " + projectPath);
                return false;
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Evaluate Test Project", "IsTestProject()", ex, $"Failed to read project file: {projectPath}");
                Logger.Warn($"Unable to read project file : " + projectPath);
                return false;
            }

            foreach (ProjectItem item in csProj.Items)
            {
                if (!item.ItemType.Equals("ProjectCapability", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                if (item.EvaluatedInclude.Equals("TestContainer", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void ParseJsonFile(string filePath, Container container)
        {
            bool isTestProject;
            try
            {
                IDictionary<string, BuildInfoComponent> components =
                    container.Components;
                LockFileFormat assetFileReader = new();
                LockFile assetFile = assetFileReader.Read(filePath);
                GetDirectDependencies(filePath);

                if (assetFile.PackageSpec != null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Logger.Debug($"ParseJsonFile():Windows Asset FileName: " + assetFile.PackageSpec.RestoreMetadata.ProjectPath);
                        isTestProject = IsTestProject(assetFile.PackageSpec.RestoreMetadata.ProjectPath);
                        container.Name = Path.GetFileName(assetFile.PackageSpec.RestoreMetadata.ProjectPath);
                        Logger.Debug($"ParseJsonFile():Windows Asset File: IsTestProject: " + isTestProject);
                    }
                    else
                    {
                        //when it's running as container
                        isTestProject = ParseJsonInContainer(filePath, ref container);
                    }

                    if (isTestProject)
                    {
                        container.Scope = ComponentScope.DevDependency;
                    }

                    foreach (LockFileTarget target in assetFile.Targets)
                    {
                        foreach (LockFileTargetLibrary library in target.Libraries)
                        {
                            ParseLibrary(library, isTestProject, components, assetFile);
                        }
                    }

                    Logger.Debug($"ParseJsonFile():Asset file found components: " + components.Count);
                }

            }
            catch (InvalidProjectFileException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("InvalidProjectFileException", "ParseJsonFile()", ex, $"File Path: {filePath}");
                Logger.Warn($"InvalidProjectFileException : While parsing project asset file : " + filePath + " Error : " + ex.Message + "\n");
            }
        }

        private static void GetDirectDependencies(string filePath)
        {
            var readValue = File.ReadAllText(filePath);
            JObject serializedContent = JObject.Parse(readValue);
            JToken projectFramworks = serializedContent["project"]["frameworks"];
            if (projectFramworks == null || !projectFramworks.HasValues)
            {
                return;
            }

            IEnumerable<JProperty> listChilds = projectFramworks.Children().OfType<JProperty>();

            //check has values
            if (listChilds != null && listChilds.ToList()[0].HasValues)
            {
                JToken projectDependencies = listChilds.ToList()[0].Value["dependencies"];
                if (projectDependencies == null)
                {
                    return;
                }
                List<JProperty> directDepCollection = new List<JProperty>();

                if (projectDependencies.HasValues)
                {
                    directDepCollection = projectDependencies.Children().OfType<JProperty>().ToList();
                }
                foreach (var child in directDepCollection)
                {
                    if (!NugetDirectDependencies.Contains(child.Name + " " + child.Value["version"]))
                    {
                        NugetDirectDependencies.Add(child.Name + " " + child.Value["version"]);
                    }
                }
            }
        }

        private static bool ParseJsonInContainer(string filePath, ref Container container)
        {
            bool isTestProject;
            string csprojFilePath = "";
            string dirName = Path.GetDirectoryName(filePath);
            if (dirName.Contains("obj"))
            {
                dirName = dirName.Replace("obj", "");
                string[] filePaths = Directory.GetFiles(dirName, "*.csproj");
                csprojFilePath = filePaths.Length > 0 ? filePaths[0] : "";
            }
            if (!string.IsNullOrEmpty(csprojFilePath) && File.Exists(csprojFilePath))
            {
                Logger.Debug($"ParseJsonFile():Linux Asset FileName: " + csprojFilePath);
                isTestProject = IsTestProject(csprojFilePath);
                container.Name = Path.GetFileName(csprojFilePath);
                Logger.Debug($"ParseJsonFile():Linux Asset File: IsTestProject: " + isTestProject);
            }
            else
            {
                Logger.Debug($"ParseJsonFile():Linux Asset FileName Not Found!! ");
                isTestProject = false;
                container.Name = Path.GetFileName(filePath);
            }

            return isTestProject;
        }

        private static void ParseLibrary(LockFileTargetLibrary library, bool isTestProject, IDictionary<string, BuildInfoComponent> components, LockFile assetFile)
        {
            if (library.Type.Equals("project", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            string id = library.Name;
            string version = library.Version.ToNormalizedString();
            NuGetComponent component = new(id, version)
            {
                Scope = isTestProject || IsDevDependecy(library) ? ComponentScope.DevDependency : ComponentScope.Required
            };

            if (components.TryGetValue(component.Id, out BuildInfoComponent existingComponent))
            {
                component = (NuGetComponent)existingComponent;
            }
            else
            {
                components.Add(component.Id, component);
            }

            GetLocalPackageHashes(component, assetFile, library);

            if (library.Dependencies is not { Count: > 0 })
            {
                return;
            }

            GetDependencies(library, component, components);
        }

        private static void GetDependencies(LockFileTargetLibrary library, NuGetComponent component, IDictionary<string, BuildInfoComponent> components)
        {
            foreach (PackageDependency dependency in library.Dependencies)
            {
                NuGetComponent depPackage = new(dependency.Id, dependency.VersionRange.OriginalString)
                {
                    Scope = component.Scope
                };

                bool exists = components.TryGetValue(depPackage.Id, out BuildInfoComponent existingDependency);
                if (exists)
                {
                    depPackage = (existingDependency as NuGetComponent)!;
                }

                //Dependencies or not adding as a componenst since it's just a
                //self-declaration of a component what that main component requires

                if (!component.Dependencies.Contains(depPackage))
                {
                    component.Dependencies.Add(depPackage);
                }

                if (!depPackage.Ancestors.Contains(component))
                {
                    depPackage.Ancestors.Add(component);
                }
            }
        }

        private static void GetLocalPackageHashes(NuGetComponent nuGetComponent, LockFile assetFile, LockFileTargetLibrary lockFileTargetLibrary)
        {
            if (!string.IsNullOrEmpty(nuGetComponent.Md5) && !string.IsNullOrEmpty(nuGetComponent.Sha1) && !string.IsNullOrEmpty(nuGetComponent.Sha256))
            {
                // all hashes are already available --> nothing to do.
                return;
            }
            foreach (LockFileLibrary lockFileLibrary in assetFile.Libraries)
            {
                if (lockFileLibrary.Name != lockFileTargetLibrary.Name || lockFileLibrary.Version != lockFileTargetLibrary.Version)
                {
                    continue;
                }

                foreach (LockFileItem packageFolder in assetFile.PackageFolders)
                {
                    CalculateHashOfPackage(nuGetComponent, packageFolder, lockFileLibrary);
                }
            }
        }

        private static void CalculateHashOfPackage(NuGetComponent nuGetComponent, LockFileItem packageFolder, LockFileLibrary lockFileLibrary)
        {
            string packagePath = Path.GetFullPath(Path.Combine(packageFolder.Path, lockFileLibrary.Path));
            if (!Directory.Exists(packagePath))
            {
                return;
            }

            IEnumerable<string> foundFiles = Directory.GetFiles(packagePath, "*.nupkg", SearchOption.TopDirectoryOnly);
            int count = foundFiles.Count();
            if (count == 0)
            {
                Logger.Error("Unable to find NuGet package " + nuGetComponent.PackageUrl + " in " + packagePath);
                LogHandlingHelper.BasicErrorHandling("Calculate hash of package", "CalculateHashOfPackage()", $"Unable to find NuGet package {nuGetComponent.PackageUrl} in {packagePath}", "");
                return;
            }

            if (count > 1)
            {
                Logger.Warn($"Found multiple NuGet packages files of : " + nuGetComponent.PackageUrl + " in : " + packagePath + "  " + JsonSerializer.Serialize(foundFiles) + "\n");
            }

            string filePath = foundFiles.First();

            nuGetComponent.Sha256 = GetFileHash(filePath, SHA256.Create());
        }

        private static string GetFileHash(string path, HashAlgorithm hashAlgorithm)
        {
            if (!File.Exists(path)) return null;

            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] hash = hashAlgorithm.ComputeHash(fileStream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
