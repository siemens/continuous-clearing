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
            if (!string.IsNullOrEmpty(CatoolLogPath) && File.Exists(CatoolLogPath))
            {
                // Output the artifact upload command
                Console.WriteLine($"##vso[artifact.upload containerfolder={LogContainerFolderName};artifactname={LogArtifactFolderName}]{CatoolLogPath}");
            }
        }

        /// <summary>
        /// Upload the BOM to the pipeline
        /// </summary>
        public void UploadBom()
        {
            if (!string.IsNullOrEmpty(CatoolBomFilePath) && File.Exists(CatoolBomFilePath))
            {
                Console.WriteLine($"##vso[artifact.upload containerfolder={BomContainerFolderName};artifactname={BomArtifactFolderName}]{CatoolBomFilePath}");
            }
        }
    }
}
