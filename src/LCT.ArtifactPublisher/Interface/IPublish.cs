// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.ArtifactPublisher.Interface
{
    /// <summary>
    /// Represents an artifact publisher.
    /// </summary>
    public interface IPublish
    {
        /// <summary>
        /// Gets or sets the path to the CATool log file.
        /// </summary>
        string CatoolLogPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the CATool BOM (Bill of Materials) file.
        /// </summary>
        string CatoolBomFilePath { get; set; }

        /// <summary>
        /// Publishes the logs.
        /// </summary>
        void UploadLogs();

        /// <summary>
        /// Publishes the BOM (Bill of Materials).
        /// </summary>
        void UploadBom();
    }
}
