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

        protected virtual void RegisterMSBuild()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
                // Log the Registred MSBUild version and path
            }
            // Log the Registered MSBuild version and path
            var instance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
            Logger.Info($"MSBuild Registered Version: {instance.Version}");
            Logger.Info($"MSBuild Registered Path: {instance.MSBuildPath}");
        }

        protected virtual Project LoadProject(string projectFilePath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            return new Project(projectFilePath, globalProperties, null, projectCollection);
        }

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
                    Logger.Info($"Processing assets file: {assetsFile}");
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

        private static void WriteDetailLog(RuntimeInfo info)
        {
            if (info == null)
            {
                Logger.Error("No runtime information available.");
                return;
            }

            Logger.Info("----- .NET Runtime Information Summary -----");

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                Logger.Error($"Error: {info.ErrorMessage}");
                if (!string.IsNullOrEmpty(info.ErrorDetails))
                    Logger.Error($"Details: {info.ErrorDetails}");
                Logger.Info("--------------------------------------------");
                return;
            }

            Logger.Info($"Project Name      : {info.ProjectName}");
            Logger.Info($"Project Path      : {info.ProjectPath}");
            Logger.Info($"SelfContained     : {info.IsSelfContained.ToString()}");
            Logger.Info($"Explicitly Set    : {info.SelfContainedExplicitlySet}");
            Logger.Info($"Evaluated Value   : {info.SelfContainedEvaluated}");
            Logger.Info($"Reason            : {info.SelfContainedReason}");

            Logger.Info("Runtime Identifiers:");
            if (info.RuntimeIdentifiers != null && info.RuntimeIdentifiers.Count > 0)
            {
                foreach (var rid in info.RuntimeIdentifiers)
                    Logger.Info($"  - {rid}");
            }
            else
            {
                Logger.Info("  (None)");
            }

            Logger.Info("Framework References:");
            if (info.FrameworkReferences != null && info.FrameworkReferences.Count > 0)
            {
                foreach (var fr in info.FrameworkReferences)
                    Logger.Info($"  - {fr.Name} (TargetingPackVersion: {fr.TargetingPackVersion})");
            }
            else
            {
                Logger.Info("  (None)");
            }

            Logger.Info("--------------------------------------------");
        }

        private static string GetProjectFilePathFromAssestJson(string assetsFile)
        {
            // Read the project.assets.json file to find the project path
            LockFileFormat assetFileReader = new();
            LockFile assetFile = assetFileReader.Read(assetsFile);
            string projectPath = assetFile?.PackageSpec?.RestoreMetadata?.ProjectPath;
            return projectPath;
        }

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