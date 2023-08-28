// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Locator;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LCT.PackageIdentifier.Model.NugetModel;
using System.Text.Json;

namespace LCT.PackageIdentifier
{
    internal class NugetDevDependencyParser
    {
        private static NugetDevDependencyParser instance = null;
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

        public List<Container> Parse(string configFile)
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

        internal static bool IsDevDependecy(LockFileTargetLibrary library)
        {
            return library.CompileTimeAssemblies.Count == 0
                && library.ContentFiles.Count == 0
                && library.EmbedAssemblies.Count == 0
                && library.FrameworkAssemblies.Count == 0
                && library.NativeLibraries.Count == 0
                && library.ResourceAssemblies.Count == 0
                && library.ToolsAssemblies.Count == 0;
        }

        internal static bool IsTestProject(string projectPath)
        {
            Project csProj;
            try
            {
                csProj = new Project(projectPath);
            }
            catch (InvalidProjectFileException ex)
            {
                Logger.Debug($"IsTestProject(): Failed to read project file : " + projectPath, ex);
                Logger.Warn($"IsTestProject: Failed to read project file : " + projectPath);
                return false;
            }
            catch (MissingFieldException ex)
            {
                Logger.Debug($"IsTestProject(): Failed to read project file : " + projectPath, ex);
                Logger.Warn($"IsTestProject: Failed to read project file : " + projectPath);
                return false;
            }
            catch (ArgumentException ex)
            {
                Logger.Debug($"IsTestProject(): Failed to read project file : " + projectPath, ex);
                Logger.Warn($"IsTestProject: Failed to read project file : " + projectPath);
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

        internal static void ParseJsonFile(string filePath, Container container)
        {
            try
            {
                IDictionary<string, BuildInfoComponent> components = container.Components;
                LockFileFormat assetFileReader = new();
                LockFile assetFile = assetFileReader.Read(filePath);
                bool isTestProject = IsTestProject(assetFile.PackageSpec.RestoreMetadata.ProjectPath);

                container.Name = Path.GetFileName(assetFile.PackageSpec.RestoreMetadata.ProjectPath);

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
            }
            catch (InvalidProjectFileException ex)
            {
                Logger.Debug($"ParseJsonFile():InvalidProjectFileException : ", ex);
                Logger.Warn($"InvalidProjectFileException : While parsing project asset file : " + filePath + " Error : " + ex.Message + "\n");
            }
            catch (NullReferenceException ex)
            {
                Logger.Debug($"ParseJsonFile(): NullReferenceException : ", ex);
                Logger.Warn($"NullReferenceException : While parsing project asset file : " + filePath + " Error : " + ex.Message + "\n");
            }
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

        internal static void GetDependencies(LockFileTargetLibrary library, NuGetComponent component, IDictionary<string, BuildInfoComponent> components)
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
                else
                {
                    components.Add(depPackage.Id, depPackage);
                }

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

        protected static void GetLocalPackageHashes(NuGetComponent nuGetComponent, LockFile assetFile, LockFileTargetLibrary lockFileTargetLibrary)
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
                return;
            }

            if (count > 1)
            {
                Logger.Warn($"Found multiple NuGet packages files of : " + nuGetComponent.PackageUrl + " in : " + packagePath + "  " + JsonSerializer.Serialize(foundFiles) + "\n");
            }

            string filePath = foundFiles.First();

            nuGetComponent.Md5 = GetFileHash(filePath, MD5.Create());
            nuGetComponent.Sha1 = GetFileHash(filePath, SHA1.Create());
            nuGetComponent.Sha256 = GetFileHash(filePath, SHA256.Create());
        }

        internal static string GetFileHash(string path, HashAlgorithm hashAlgorithm)
        {
            if (!File.Exists(path)) return null;

            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] hash = hashAlgorithm.ComputeHash(fileStream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
