// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace ArtifactoryUploader.Constants
{
    /// <summary>
    /// Artifactory Constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ArtifactoryConstant
    {
        #region Fields

        public const string Log4netArtifactoryUploaderConfigFileName = "log4netArtifactoryUploader.config";
        public const string Cargo = "cargo";

        /// <summary>
        /// The JFrog item property name used to record which tool uploaded the artifact.
        /// </summary>
        public const string UploadedByProperty = "uploadedby";

        /// <summary>
        /// The value written to the <see cref="UploadedByProperty"/> property.
        /// </summary>
        public const string UploadedByValue = "SIT.Upload";
        #endregion
    }
}
