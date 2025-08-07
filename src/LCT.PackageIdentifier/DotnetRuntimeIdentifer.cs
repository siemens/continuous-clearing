// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using log4net;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Directory = System.IO.Directory;

namespace LCT.PackageIdentifier
{
    internal class DotnetRuntimeIdentifer : IRuntimeIdentifier
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Registers the default MSBuild instance for use within the application if it has not already been registered.
        /// </summary>
        /// <remarks>This method ensures that MSBuild is available for build-related operations by
        /// registering the default instance if necessary. If MSBuild is already registered, no action is taken. After
        /// registration, information about the MSBuild version and path is logged for diagnostic purposes. Derived
        /// classes can override this method to customize the MSBuild registration process.</remarks>
        protected virtual void RegisterMSBuild()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
            // Log the Registered MSBuild version and path
            var instance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
            Logger.Info($"MSBuild Registered Version: {instance.Version}");
            Logger.Debug($"MSBuild Registered Path: {instance.MSBuildPath}");
        }

        /// <summary>
        /// Loads a project from the specified file path using the provided global properties and project collection.
        /// </summary>
        /// <param name="projectFilePath">The full path to the project file to load. Cannot be <c>null</c> or empty.</param>
        /// <param name="globalProperties">A dictionary of global properties to apply to the project during loading. May be <c>null</c> if no global
        /// properties are required.</param>
        /// <param name="projectCollection">The <see cref="ProjectCollection"/> instance to associate with the loaded project. Cannot be <c>null</c>.</param>
        /// <returns>A <see cref="Project"/> instance representing the loaded project.</returns>
        protected virtual Project LoadProject(string projectFilePath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            return new Project(projectFilePath, globalProperties, null, projectCollection);
        }


        /// <summary>
        /// Identifies the runtime information for a .NET project based on the specified application settings.
        /// </summary>
        /// <remarks>This method scans the input directory for NuGet asset files and attempts to determine
        /// the runtime configuration by analyzing associated project files. If multiple valid runtime configurations
        /// are found, a self-contained runtime with framework references is prioritized. If no valid project files are
        /// found, the returned <see cref="RuntimeInfo"/> will contain an error message describing the issue.</remarks>
        /// <param name="appSettings">The application settings that specify the input directory and NuGet exclusion rules used to locate and
        /// filter asset files.</param>
        /// <returns>A <see cref="RuntimeInfo"/> instance containing details about the identified runtime configuration. If no
        /// valid runtime is found or an error occurs, the returned object will include an error message describing the
        /// problem.</returns>
        public RuntimeInfo IdentifyRuntime(CommonAppSettings appSettings)
        {
            var info = new RuntimeInfo();

            try
            {
                // Register MSBuild Locator
                RegisterMSBuild();
                var assetsFiles = Directory.GetFiles(appSettings.Directory.InputFolder, FileConstant.NugetAssetFile, SearchOption.AllDirectories);
                if (assetsFiles.Length == 0)
                {
                    info.ErrorMessage = "No " + FileConstant.NugetAssetFile + " files found in the specified directory.";
                    WriteDetailLog(info);
                    return info;
                }

                // Filter out excluded files based on appSettings
                assetsFiles = assetsFiles.Where(file => !IsExcluded(file, appSettings.Nuget?.Exclude)).ToArray();

                if (assetsFiles.Length == 0)
                {
                    info.ErrorMessage = "No " + FileConstant.NugetAssetFile + " files found in the specified directory.";
                    WriteDetailLog(info);
                    return info;
                }

                List<RuntimeInfo> runtimeInfos = [];

                foreach (var assetsFile in assetsFiles)
                {
                    Logger.Debug($"Processing assets file: {assetsFile}");
                    var csprojFilePath = GetProjectFilePathFromAssestJson(assetsFile);

                    if (!string.IsNullOrWhiteSpace(csprojFilePath) && File.Exists(csprojFilePath))
                    {
                        RuntimeInfo runtimeInfo = ParseCSProjFile(csprojFilePath);
                        if (runtimeInfo != null)
                        {
                            runtimeInfos.Add(runtimeInfo);
                        }

                        if (runtimeInfo != null && runtimeInfo.IsSelfContained && runtimeInfo.FrameworkReferences.Count > 0)
                        {
                            return runtimeInfo;
                        }
                    }
                }

                return runtimeInfos.FirstOrDefault() ?? new RuntimeInfo
                {
                    ErrorMessage = "No valid project files found in the assets files."
                };

            }
            catch (Exception ex)
            {
                info.ErrorMessage = "Error registering MSBuildLocator or reading assets files";
                info.ErrorDetails = ex.ToString();
                WriteDetailLog(info);
                return info;
            }
        }

        /// <summary>
        /// Parses a .csproj project file and extracts runtime-related information, including target framework,
        /// self-contained status, runtime identifiers, and framework references.
        /// </summary>
        /// <remarks>The method analyzes the specified project file to determine properties such as target
        /// framework, self-contained deployment status, runtime identifiers, and framework references relevant to
        /// runtime behavior. If the project file is invalid or cannot be loaded, error details are provided in the
        /// result.</remarks>
        /// <param name="projectFilePath">The full path to the .csproj file to parse. Must refer to an existing file; otherwise, an error message is
        /// returned in the result.</param>
        /// <returns>A <see cref="RuntimeInfo"/> object containing details about the project's runtime configuration. If the file
        /// path is invalid or an error occurs during parsing, the returned object includes error information.</returns>
        private RuntimeInfo ParseCSProjFile(string projectFilePath)
        {
            var info = new RuntimeInfo();

            if (string.IsNullOrWhiteSpace(projectFilePath) || !File.Exists(projectFilePath))
            {
                info.ErrorMessage = $"Invalid file path or file does not exist: {projectFilePath}";
                WriteDetailLog(info);
                return info;
            }

            ProjectCollection projectCollection = null;
            Project project = null;

            try
            {
                projectCollection = new ProjectCollection();
                var globalProperties = new Dictionary<string, string>
                {
                    { "Configuration", "Release" },
                    { "Platform", "AnyCPU" }
                };

                project = LoadProject(projectFilePath, globalProperties, projectCollection);

                info.ProjectPath = project.FullPath;
                info.ProjectName = Path.GetFileNameWithoutExtension(project.FullPath);
                string targetFrameworkValue = project.GetPropertyValue("TargetFramework");

                // Self-contained status
                string selfContainedEvaluated = project.GetPropertyValue("SelfContained");
                string runtimeIdentifier = project.GetPropertyValue("RuntimeIdentifier");
                string runtimeIdentifiers = project.GetPropertyValue("RuntimeIdentifiers");

                bool isSelfContained = false;
                bool selfContainedExplicitlySet = project.GetProperty("SelfContained") != null;
                bool selfContainedValueParsed = bool.TryParse(selfContainedEvaluated, out isSelfContained);
                bool hasRuntimeIdentifier = !string.IsNullOrEmpty(runtimeIdentifier) || !string.IsNullOrEmpty(runtimeIdentifiers);

                info.SelfContainedEvaluated = selfContainedEvaluated;
                info.IsSelfContained = isSelfContained;
                info.SelfContainedExplicitlySet = selfContainedExplicitlySet;
                info.RuntimeIdentifiers = new List<string>();
                if (!string.IsNullOrEmpty(runtimeIdentifier))
                    info.RuntimeIdentifiers.Add(runtimeIdentifier);
                if (!string.IsNullOrEmpty(runtimeIdentifiers))
                    info.RuntimeIdentifiers.AddRange(runtimeIdentifiers.Split(';', StringSplitOptions.RemoveEmptyEntries));

                if (selfContainedValueParsed)
                {
                    if (isSelfContained)
                    {
                        if (selfContainedExplicitlySet)
                            info.SelfContainedReason = "'SelfContained' property is explicitly set to 'true'.";
                        else if (hasRuntimeIdentifier)
                            info.SelfContainedReason = "'SelfContained' property implicitly defaulted to 'true' because 'RuntimeIdentifier(s)' is specified for an executable project.";
                        else
                            info.SelfContainedReason = "'SelfContained' property implicitly defaulted to 'true' (uncommon without RID, check SDK defaults).";
                    }
                    else
                    {
                        if (selfContainedExplicitlySet)
                            info.SelfContainedReason = "'SelfContained' property is explicitly set to 'false'.";
                        else
                            info.SelfContainedReason = "'SelfContained' property implicitly defaulted to 'false' (no explicit setting and no RuntimeIdentifier, or not an executable).";
                    }
                }
                else
                {
                    info.SelfContainedReason = "Warning: 'SelfContained' property could not be parsed as a boolean. Its evaluated value is unexpected.";
                }

                // Framework references
                info.FrameworkReferences = new List<FrameworkReferenceInfo>();
                foreach (var item in project.Items.Where(i => i.ItemType == "KnownFrameworkReference"))
                {
                    if (item.GetMetadataValue("TargetFramework") == targetFrameworkValue)
                    {
                        string targetingPackVersion = item.GetMetadataValue("TargetingPackVersion");
                        info.FrameworkReferences.Add(new FrameworkReferenceInfo
                        {
                            TargetFramework = targetFrameworkValue,
                            Name = item.EvaluatedInclude,
                            TargetingPackVersion = targetingPackVersion
                        });
                        // We only need the first matching framework reference for the target framework
                        break;
                    }
                }
            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException ex)
            {
                info.ErrorMessage = $"Error loading project file: {ex.Message}";
                info.ErrorDetails = $"Details: {ex.ErrorCode} at {ex.LineNumber}, {ex.ColumnNumber}";
            }
            catch (Exception ex)
            {
                info.ErrorMessage = "An unexpected error occurred";
                info.ErrorDetails = ex.ToString();
            }
            finally
            {
                if (project != null)
                {
                    projectCollection?.UnloadProject(project);
                }
                projectCollection?.Dispose();
            }

            WriteDetailLog(info);
            return info;
        }

        /// <summary>
        /// Writes a detailed summary of the specified .NET runtime information to the application log.
        /// </summary>
        /// <remarks>The log output includes project metadata, self-contained deployment settings, runtime identifiers,
        /// and framework references. If <paramref name="info"/> contains an error message, only error details are logged. This
        /// method is intended for diagnostic and troubleshooting purposes.</remarks>
        /// <param name="info">The <see cref="RuntimeInfo"/> object containing runtime details to be logged.  If <paramref name="info"/> is <see
        /// langword="null"/>, an error message is logged and no further information is written.</param>
        private static void WriteDetailLog(RuntimeInfo info)
        {
            if (info == null)
            {
                Logger.Error("No runtime information available.");
                return;
            }

            Logger.Debug("----- .NET Runtime Information Summary -----");

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                Logger.Error($"Error: {info.ErrorMessage}");
                if (!string.IsNullOrEmpty(info.ErrorDetails))
                    Logger.Error($"Details: {info.ErrorDetails}");
                Logger.Debug("--------------------------------------------");
                return;
            }

            Logger.Debug($"Project Name      : {info.ProjectName}");
            Logger.Debug($"Project Path      : {info.ProjectPath}");
            Logger.Debug($"SelfContained     : {info.IsSelfContained.ToString()}");
            Logger.Debug($"Explicitly Set    : {info.SelfContainedExplicitlySet}");
            Logger.Debug($"Evaluated Value   : {info.SelfContainedEvaluated}");
            Logger.Debug($"Reason            : {info.SelfContainedReason}");

            Logger.Debug("Runtime Identifiers:");
            if (info.RuntimeIdentifiers != null && info.RuntimeIdentifiers.Count > 0)
            {
                foreach (var rid in info.RuntimeIdentifiers)
                    Logger.Debug($"  - {rid}");
            }
            else
            {
                Logger.Debug("  (None)");
            }

            Logger.Debug("Framework References:");
            if (info.FrameworkReferences != null && info.FrameworkReferences.Count > 0)
            {
                foreach (var fr in info.FrameworkReferences)
                    Logger.Debug($"  - {fr.Name} (TargetingPackVersion: {fr.TargetingPackVersion})");
            }
            else
            {
                Logger.Debug("  (None)");
            }

            Logger.Debug("--------------------------------------------");
        }

        /// <summary>
        /// Retrieves the project file path referenced in the specified <c>project.assets.json</c> file.
        /// </summary>
        /// <param name="assetsFile">The path to the <c>project.assets.json</c> file from which to extract the project file path. Must not be
        /// <see langword="null"/> or empty.</param>
        /// <returns>The full path to the project file as specified in the assets file, or <see langword="null"/> if the path
        /// cannot be determined.</returns>
        private static string GetProjectFilePathFromAssestJson(string assetsFile)
        {
            // Read the project.assets.json file to find the project path
            LockFileFormat assetFileReader = new();
            LockFile assetFile = assetFileReader.Read(assetsFile);
            string projectPath = assetFile?.PackageSpec?.RestoreMetadata?.ProjectPath;
            return projectPath;
        }

        /// <summary>
        /// Determines whether the specified file path matches any of the provided exclusion patterns.
        /// </summary>
        /// <param name="filePath">The file path to evaluate against the exclusion patterns. Cannot be <c>null</c>.</param>
        /// <param name="excludePatterns">An array of string patterns to check for exclusion. If <paramref name="excludePatterns"/> is <c>null</c> or
        /// empty, no exclusion is applied.</param>
        /// <returns><see langword="true"/> if <paramref name="filePath"/> contains any of the <paramref name="excludePatterns"/>
        /// (case-insensitive); otherwise, <see langword="false"/>.</returns>
        private static bool IsExcluded(string filePath, string[] excludePatterns)
        {
            if (excludePatterns == null || excludePatterns.Length == 0)
            {
                return false;
            }

            return excludePatterns.Any(pattern => filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }

}