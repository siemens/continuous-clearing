// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Runtime;
using log4net;
using System;
using System.IO;
using System.Reflection;

namespace LCT.Common
{
    public static class PipelineArtifactUploader
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public const string LogArtifactFolderName = "ContinuousClearing_Log";
        public const string BomArtifactFolderName = "ContinuousClearing_Bom";
        public const string LogContainerFolderName = "Container_Log";
        public const string BomContainerFolderName = "Container_Bom";

        public static void UploadArtifacts()
        {            
            UploadBom();
            UploadLogs();
        }

        /// <summary>
        /// Upload the Logs to the pipeline
        /// </summary>
        public static void UploadLogs()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            if (envType == EnvironmentType.AzurePipeline
                            && !string.IsNullOrEmpty(Log4Net.CatoolLogPath)
                            && File.Exists(Log4Net.CatoolLogPath)
                            && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                LogManager.Shutdown();
                Logger.DebugFormat("Uploading artifact log file path: {0}", Log4Net.CatoolLogPath);
                Logger.Debug("====================<<<<< Exit >>>>>====================");
                Console.WriteLine($"##vso[artifact.upload containerfolder={LogContainerFolderName};artifactname={LogArtifactFolderName}]{Log4Net.CatoolLogPath}");
            }
            else if (envType == EnvironmentType.Unknown)
            {
                Logger.Warn($"Uploading of logs is not supported.");
                Logger.Debug("====================<<<<< Exit >>>>>====================");
            }

        }

        /// <summary>
        /// Upload the BOM to the pipeline
        /// </summary>
        public static void UploadBom()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            if (envType == EnvironmentType.AzurePipeline
                            && !string.IsNullOrEmpty(FileOperations.CatoolBomFilePath)
                            && File.Exists(FileOperations.CatoolBomFilePath)
                            && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                Logger.DebugFormat("Uploading artifact Bom file path: {0}", FileOperations.CatoolBomFilePath);
                Console.WriteLine($"##vso[artifact.upload containerfolder={BomContainerFolderName};artifactname={BomArtifactFolderName}]{FileOperations.CatoolBomFilePath}");
            }
            else if (envType == EnvironmentType.Unknown)
            {
                Logger.Warn($"Uploading of SBOM is not supported.");
            }

        }

        /// <summary>
        /// Prints a warning message to the console in a format suitable for Azure Pipelines.
        /// </summary>
        /// <remarks>This method formats the warning message specifically for Azure Pipelines when the
        /// application  is running in that environment and not inside a container. If these conditions are not met, 
        /// the method does not produce any output.</remarks>
        /// <param name="content">The warning message to be displayed. Cannot be null or empty.</param>
        public static void PrintWarning(string content)
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            if (envType == EnvironmentType.AzurePipeline
                            && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                Console.WriteLine($"##[warning]{content}");
            }
        }
    }
}
