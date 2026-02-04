// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents the root SPDX BOM data structure.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SpdxBomData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SPDX version.
        /// </summary>
        [JsonProperty("spdxVersion")]
        public string SpdxVersion { get; set; }

        /// <summary>
        /// Gets or sets the data license.
        /// </summary>
        [JsonProperty("dataLicense")]
        public string DataLicense { get; set; }

        /// <summary>
        /// Gets or sets the SPDX identifier.
        /// </summary>
        public string SPDXID { get; set; }

        /// <summary>
        /// Gets or sets the document name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the document namespace.
        /// </summary>
        [JsonProperty("documentNamespace")]
        public string DocumentNamespace { get; set; }

        /// <summary>
        /// Gets or sets the creation information.
        /// </summary>
        [JsonProperty("creationInfo")]
        public CreationInfo CreationInfo { get; set; }

        /// <summary>
        /// Gets or sets the list of packages.
        /// </summary>
        [JsonProperty("packages")]
        public List<Package> Packages { get; set; }

        /// <summary>
        /// Gets or sets the list of files.
        /// </summary>
        [JsonProperty("files")]
        public List<File> Files { get; set; }

        /// <summary>
        /// Gets or sets the list of relationships.
        /// </summary>
        [JsonProperty("relationships")]
        public List<Relationship> Relationships { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a checksum for verifying file integrity.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Checksum
    {
        #region Properties

        /// <summary>
        /// Gets or sets the checksum algorithm.
        /// </summary>
        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the checksum value.
        /// </summary>
        [JsonProperty("checksumValue")]
        public string ChecksumValue { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents creation information for the SPDX document.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CreationInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the license list version.
        /// </summary>
        [JsonProperty("licenseListVersion")]
        public string LicenseListVersion { get; set; }

        /// <summary>
        /// Gets or sets the list of creators.
        /// </summary>
        [JsonProperty("creators")]
        public List<string> Creators { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time.
        /// </summary>
        [JsonProperty("created")]
        public DateTime Created { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents an external reference for a package.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ExternalRef
    {
        #region Properties

        /// <summary>
        /// Gets or sets the reference category.
        /// </summary>
        [JsonProperty("referenceCategory")]
        public string ReferenceCategory { get; set; }

        /// <summary>
        /// Gets or sets the reference type.
        /// </summary>
        [JsonProperty("referenceType")]
        public string ReferenceType { get; set; }

        /// <summary>
        /// Gets or sets the reference locator.
        /// </summary>
        [JsonProperty("referenceLocator")]
        public string ReferenceLocator { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a file in the SPDX document.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class File
    {
        #region Properties

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the SPDX identifier for the file.
        /// </summary>
        public string SPDXID { get; set; }

        /// <summary>
        /// Gets or sets the list of file types.
        /// </summary>
        [JsonProperty("fileTypes")]
        public List<string> FileTypes { get; set; }

        /// <summary>
        /// Gets or sets the list of checksums for the file.
        /// </summary>
        [JsonProperty("checksums")]
        public List<Checksum> Checksums { get; set; }

        /// <summary>
        /// Gets or sets the concluded license.
        /// </summary>
        [JsonProperty("licenseConcluded")]
        public string LicenseConcluded { get; set; }

        /// <summary>
        /// Gets or sets the license information found in the file.
        /// </summary>
        [JsonProperty("licenseInfoInFiles")]
        public List<string> LicenseInfoInFiles { get; set; }

        /// <summary>
        /// Gets or sets the copyright text.
        /// </summary>
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a package in the SPDX document.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Package
    {
        #region Properties

        /// <summary>
        /// Gets or sets the package name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SPDX identifier for the package.
        /// </summary>
        public string SPDXID { get; set; }

        /// <summary>
        /// Gets or sets the version information.
        /// </summary>
        [JsonProperty("versionInfo")]
        public string VersionInfo { get; set; }

        /// <summary>
        /// Gets or sets the supplier information.
        /// </summary>
        [JsonProperty("supplier")]
        public string Supplier { get; set; }

        /// <summary>
        /// Gets or sets the download location.
        /// </summary>
        [JsonProperty("downloadLocation")]
        public string DownloadLocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether files were analyzed.
        /// </summary>
        [JsonProperty("filesAnalyzed")]
        public bool FilesAnalyzed { get; set; }

        /// <summary>
        /// Gets or sets the list of checksums for the package.
        /// </summary>
        [JsonProperty("checksums")]
        public List<Checksum> Checksums { get; set; }

        /// <summary>
        /// Gets or sets the source information.
        /// </summary>
        [JsonProperty("sourceInfo")]
        public string SourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the concluded license.
        /// </summary>
        [JsonProperty("licenseConcluded")]
        public string LicenseConcluded { get; set; }

        /// <summary>
        /// Gets or sets the declared license.
        /// </summary>
        [JsonProperty("licenseDeclared")]
        public string LicenseDeclared { get; set; }

        /// <summary>
        /// Gets or sets the copyright text.
        /// </summary>
        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        /// <summary>
        /// Gets or sets the list of external references.
        /// </summary>
        [JsonProperty("externalRefs")]
        public List<ExternalRef> ExternalRefs { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a relationship between SPDX elements.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Relationship
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SPDX element identifier.
        /// </summary>
        [JsonProperty("spdxElementId")]
        public string SpdxElementId { get; set; }

        /// <summary>
        /// Gets or sets the related SPDX element identifier.
        /// </summary>
        [JsonProperty("relatedSpdxElement")]
        public string RelatedSpdxElement { get; set; }

        /// <summary>
        /// Gets or sets the relationship type.
        /// </summary>
        [JsonProperty("relationshipType")]
        public string RelationshipType { get; set; }

        #endregion
    }
}
