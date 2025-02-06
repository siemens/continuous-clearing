using LCT.Common.Runtime;
using log4net;
using log4net.Core;
using System;
using System.IO;
using System.Reflection;

namespace LCT.Common
{
    public static class PipelineArtifactUploader
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public const string LogArtifactFolderName = "ContinuousClearing_Log";
        public const string BomArtifactFolderName = "ContinuousClearing_Bom";
        public const string LogContainerFolderName = "Container_Log";
        public const string BomContainerFolderName = "Container_Bom";

        public static void UploadArtifacts()
        {
            UploadLogs();
            UploadBom();
        }

        /// <summary>
        /// Upload the Logs to the pipeline
        /// </summary>
        public static void UploadLogs()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            LogManager.Shutdown();
            if (envType == EnvironmentType.AzurePipeline && !string.IsNullOrEmpty(Log4Net.CatoolLogPath) && File.Exists(Log4Net.CatoolLogPath))
            {                
                Console.WriteLine($"##vso[artifact.upload containerfolder={LogContainerFolderName};artifactname={LogArtifactFolderName}]{Log4Net.CatoolLogPath}");
            }
            else if (envType == EnvironmentType.Unknown)
            {
                Logger.Logger.Log(null, Level.Alert, $"Uploading of logs is not supported.", null);                
            }

        }

        /// <summary>
        /// Upload the BOM to the pipeline
        /// </summary>
        public static void UploadBom()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            LogManager.Shutdown();
            if (envType == EnvironmentType.AzurePipeline && !string.IsNullOrEmpty(FileOperations.CatoolBomFilePath) && File.Exists(FileOperations.CatoolBomFilePath))
            {
                Console.WriteLine($"##vso[artifact.upload containerfolder={BomContainerFolderName};artifactname={BomArtifactFolderName}]{FileOperations.CatoolBomFilePath}");
            }
            else if (envType == EnvironmentType.Unknown)
            {
                Logger.Logger.Log(null, Level.Alert, $"Uploading of SBOM is not supported.", null);
            }

        }
    }
}
