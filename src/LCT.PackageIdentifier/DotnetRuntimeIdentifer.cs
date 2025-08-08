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
    public class DotnetRuntimeIdentifer : IRuntimeIdentifier
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Registers the default MSBuild instance for use within the application if it has not already been registered.
        /// </summary>
        /// <remarks>This method ensures that MSBuild is available for build-related operations by
        /// registering the default instance if necessary. If MSBuild is already registered, no action is taken. After
        /// registration, information about the MSBuild version and path is logged for diagnostic purposes. Derived
        /// classes can override this method to customize the MSBuild registration process.</remarks>
        public virtual void Register()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
            // Log the Registered MSBuild version and path
            var instance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
            Logger.Info($"MSBuild Registered Version: {instance?.Version}");
            Logger.Debug($"MSBuild Registered Path: {instance?.MSBuildPath}");
        }

        /// <summary>
        /// Loads a project from the specified file path using the provided global properties and project collection.
        /// </summary>
        /// <param name="projectFilePath">The full path to the project file to load. Cannot be <c>null</c> or empty.</param>
        /// <param name="globalProperties">A dictionary of global properties to apply to the project during loading. May be <c>null</c> if no global
        /// properties are required.</param>
        /// <param name="projectCollection">The <see cref="ProjectCollection"/> instance to associate with the loaded project. Cannot be <c>null</c>.</param>
        /// <returns>A <see cref="Project"/> instance representing the loaded project.</returns>
        public Project LoadProject(string projectFilePath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
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
            catch (DirectoryNotFoundException ex)
            {
                info.ErrorMessage = "Directory not found";
                info.ErrorDetails = $"The specified directory '{appSettings.Directory?.InputFolder}' was not found. Error: {ex.Message}";
                WriteDetailLog(info);
                return info;
            }
            catch (FileNotFoundException ex)
            {
                info.ErrorMessage = "File not found";
                info.ErrorDetails = $"A required file could not be found. Error: {ex.Message}";
                WriteDetailLog(info);
                return info;
            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException ex)
            {
                info.ErrorMessage = "Invalid project file";
                info.ErrorDetails = $"Error parsing project file: {ex.Message}";
                WriteDetailLog(info);
                return info;
            }
        }

        #region Private Methods
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

            try
            {
                using var projectCollection = new ProjectCollection();
                var project = LoadProjectWithDefaultSettings(projectFilePath, projectCollection);

                PopulateProjectInfo(info, project);
                ProcessSelfContainedStatus(info, project);
                ProcessRuntimeIdentifiers(info, project);
                ProcessFrameworkReferences(info, project);
            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException ex)
            {
                HandleProjectFileException(info, ex);
            }
            catch (ArgumentNullException ex)
            {
                info.ErrorMessage = "Argument null exception";
                info.ErrorDetails = $"An argument was null when it should not have been. Error: {ex.Message}";
                WriteDetailLog(info);
                return info;
            }

            WriteDetailLog(info);
            return info;
        }

        /// <summary>
        /// Loads a project with default settings for configuration and platform.
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <param name="projectCollection"></param>
        /// <returns></returns>
        private Project LoadProjectWithDefaultSettings(string projectFilePath, ProjectCollection projectCollection)
        {
            var globalProperties = new Dictionary<string, string>
            {
                { "Configuration", "Release" },
                { "Platform", "AnyCPU" }
            };

            return LoadProject(projectFilePath, globalProperties, projectCollection);
        }

        /// <summary>
        /// Populates the <see cref="RuntimeInfo"/> object with project metadata such as path and name.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="project"></param>
        private void PopulateProjectInfo(RuntimeInfo info, Project project)
        {
            info.ProjectPath = project.FullPath;
            info.ProjectName = Path.GetFileNameWithoutExtension(project.FullPath);
        }

        /// <summary>
        /// Processes the self-contained status of the project, determining whether it is self-contained
        /// </summary>
        /// <param name="info"></param>
        /// <param name="project"></param>
        private void ProcessSelfContainedStatus(RuntimeInfo info, Project project)
        {
            string selfContainedEvaluated = project.GetPropertyValue("SelfContained");
            string runtimeIdentifier = project.GetPropertyValue("RuntimeIdentifier");
            string runtimeIdentifiers = project.GetPropertyValue("RuntimeIdentifiers");

            bool selfContainedExplicitlySet = project.GetProperty("SelfContained") != null;
            bool.TryParse(selfContainedEvaluated, out bool isSelfContained);
            bool hasRuntimeIdentifier = !string.IsNullOrEmpty(runtimeIdentifier) || !string.IsNullOrEmpty(runtimeIdentifiers);

            info.SelfContainedEvaluated = selfContainedEvaluated;
            info.IsSelfContained = isSelfContained;
            info.SelfContainedExplicitlySet = selfContainedExplicitlySet;

            SetSelfContainedReason(info, isSelfContained, selfContainedExplicitlySet, hasRuntimeIdentifier,
                !string.IsNullOrEmpty(selfContainedEvaluated));
        }

        /// <summary>
        /// Sets the reason for the self-contained status based on the evaluated value and project properties.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="isSelfContained"></param>
        /// <param name="selfContainedExplicitlySet"></param>
        /// <param name="hasRuntimeIdentifier"></param>
        /// <param name="selfContainedValueParsed"></param>
        private void SetSelfContainedReason(RuntimeInfo info, bool isSelfContained, bool selfContainedExplicitlySet,
            bool hasRuntimeIdentifier, bool selfContainedValueParsed)
        {
            if (!selfContainedValueParsed)
            {
                info.SelfContainedReason = "Warning: 'SelfContained' property could not be parsed as a boolean. Its evaluated value is unexpected.";
                return;
            }

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

        /// <summary>
        /// Processes the runtime identifiers from the project file, extracting both single and multiple runtime identifiers.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="project"></param>
        private void ProcessRuntimeIdentifiers(RuntimeInfo info, Project project)
        {
            string runtimeIdentifier = project.GetPropertyValue("RuntimeIdentifier");
            string runtimeIdentifiers = project.GetPropertyValue("RuntimeIdentifiers");

            if (!string.IsNullOrEmpty(runtimeIdentifier))
                info.RuntimeIdentifiers.Add(runtimeIdentifier);

            if (!string.IsNullOrEmpty(runtimeIdentifiers))
                info.RuntimeIdentifiers.AddRange(runtimeIdentifiers.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Processes the framework references from the project file, extracting relevant information such as target framework and targeting pack version.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="project"></param>
        private void ProcessFrameworkReferences(RuntimeInfo info, Project project)
        {
            string targetFrameworkValue = project.GetPropertyValue("TargetFramework");
            var frameworkReferences = project.Items
                .Where(i => i.ItemType == "KnownFrameworkReference" &&
                           i.GetMetadataValue("TargetFramework") == targetFrameworkValue)
                .Take(1) // We only need the first matching framework reference
                .Select(item => new FrameworkReferenceInfo
                {
                    TargetFramework = targetFrameworkValue,
                    Name = item.EvaluatedInclude,
                    TargetingPackVersion = item.GetMetadataValue("TargetingPackVersion")
                });

            info.FrameworkReferences.AddRange(frameworkReferences);
        }

        /// <summary>
        /// Handles exceptions related to invalid project files, populating the provided <see cref="RuntimeInfo"/> with error details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        private void HandleProjectFileException(RuntimeInfo info, Microsoft.Build.Exceptions.InvalidProjectFileException ex)
        {
            info.ErrorMessage = $"Error loading project file: {ex.Message}";
            info.ErrorDetails = $"Details: {ex.ErrorCode} at {ex.LineNumber}, {ex.ColumnNumber}";
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
        #endregion
    }
}