﻿// --------------------------------------------------------------------------------------------------------------------
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
        public const string ContainerFolderName = "Container";

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
            try
            {
                // Output the artifact upload command
                Console.WriteLine($"##vso[artifact.upload containerfolder={ContainerFolderName};artifactname={LogArtifactFolderName}]{CatoolLogPath}");
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Upload the BOM to the pipeline
        /// </summary>
        public void UploadBom()
        {
            try
            {
                if (!string.IsNullOrEmpty(CatoolBomFilePath) && Directory.Exists(CatoolBomFilePath))
                {
                    Console.WriteLine($"##vso[artifact.upload containerfolder={ContainerFolderName};artifactname={BomArtifactFolderName}]{CatoolBomFilePath}");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}