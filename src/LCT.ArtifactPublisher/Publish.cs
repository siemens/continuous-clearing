// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.ArtifactPublisher.Interface;

namespace LCT.ArtifactPublisher
{
    /// <summary>
    /// Publishes the artifacts to the pipeline
    /// </summary>
    public class Publish : IPublish
    {
        public string CatoolLogPath { get; set; }
        public string CatoolBomFilePath { get; set; }
        public const string LogArtifactFolderName = "ContinuousClearing_Log";
        public const string BomArtifactFolderName = "ContinuousClearing_Bom";
        public const string LogContainerFolderName = "Container_Log";
        public const string BomContainerFolderName = "Container_Bom";

        /// <summary>
        /// constructor method of artifact publisher class, initializes the params
        /// </summary>
        /// <param name="catoolLogPath"></param>
        /// <param name="catoolBomfilePath"></param>
        public Publish(string catoolLogPath, string catoolBomfilePath)
        {
            CatoolLogPath = catoolLogPath;
            CatoolBomFilePath = catoolBomfilePath;
        }

        /// <summary>
        /// Uploads the logs to the pipeline
        /// </summary>
        public void UploadLogs()
        {
            var envType = GetEnvironment();
            if (envType== EnvironmentType.AzurePipeline)
            {
                if (!string.IsNullOrEmpty(CatoolLogPath) && File.Exists(CatoolLogPath))
                {
                    // Output the artifact upload command
                    Console.WriteLine($"##vso[artifact.upload containerfolder={LogContainerFolderName};artifactname={LogArtifactFolderName}]{CatoolLogPath}");
                }
            }else if (envType == EnvironmentType.Unknown)
            {
                Console.WriteLine("Uploading of SBOM and the logs are not supported.");
            }
           
        }

        /// <summary>
        /// Upload the BOM to the pipeline
        /// </summary>
        public void UploadBom()
        {
            var envType = GetEnvironment();
            if (envType == EnvironmentType.AzurePipeline)
            {
                if (!string.IsNullOrEmpty(CatoolBomFilePath) && File.Exists(CatoolBomFilePath))
                {
                    Console.WriteLine($"##vso[artifact.upload containerfolder={BomContainerFolderName};artifactname={BomArtifactFolderName}]{CatoolBomFilePath}");
                }
            }
            else if (envType == EnvironmentType.Unknown)
            {
                Console.WriteLine("Uploading of SBOM and the logs are not supported.");
            }
           
        }
        public static EnvironmentType GetEnvironment()
        {
            // Azure Release Pipeline contains both "Release_ReleaseId" and
            // "Build_BuildId". Therefore we need to check first for "Release_ReleaseId".
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/release/variables
            if (IsEnvironmentVariableDefined("Release_ReleaseId"))
            {
                return EnvironmentType.AzureRelease;
            }

            // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables
            if (IsEnvironmentVariableDefined("Build_BuildId"))
            {
                return EnvironmentType.AzurePipeline;
            }

            // https://docs.gitlab.com/ce/ci/variables/predefined_variables.html
            if (IsEnvironmentVariableDefined("CI_JOB_ID"))
            {
                return EnvironmentType.GitLab;
            }

            return EnvironmentType.Unknown;
        }

        public static bool IsEnvironmentVariableDefined(string name)
        {
            string value = Environment.GetEnvironmentVariable(name) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        public enum EnvironmentType
        {
            Unknown = 0,
            GitLab = 1,
            AzurePipeline = 2,
            AzureRelease = 3
        }
        
    }
}
