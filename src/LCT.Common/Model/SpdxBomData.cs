// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Model
{
    [ExcludeFromCodeCoverage]
    public class SpdxBomData
    {
        [JsonProperty("spdxVersion")]
        public string SpdxVersion { get; set; }

        [JsonProperty("dataLicense")]
        public string DataLicense { get; set; }

        public string SPDXID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("documentNamespace")]
        public string DocumentNamespace { get; set; }

        [JsonProperty("creationInfo")]
        public CreationInfo CreationInfo { get; set; }

        [JsonProperty("packages")]
        public List<Package> Packages { get; set; }

        [JsonProperty("files")]
        public List<File> Files { get; set; }

        [JsonProperty("relationships")]
        public List<Relationship> Relationships { get; set; }
    }

    public class Checksum
    {
        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        [JsonProperty("checksumValue")]
        public string ChecksumValue { get; set; }
    }

    public class CreationInfo
    {
        [JsonProperty("licenseListVersion")]
        public string LicenseListVersion { get; set; }

        [JsonProperty("creators")]
        public List<string> Creators { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }
    }

    public class ExternalRef
    {
        [JsonProperty("referenceCategory")]
        public string ReferenceCategory { get; set; }

        [JsonProperty("referenceType")]
        public string ReferenceType { get; set; }

        [JsonProperty("referenceLocator")]
        public string ReferenceLocator { get; set; }
    }

    public class File
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        
        public string SPDXID { get; set; }

        [JsonProperty("fileTypes")]
        public List<string> FileTypes { get; set; }

        [JsonProperty("checksums")]
        public List<Checksum> Checksums { get; set; }

        [JsonProperty("licenseConcluded")]
        public string LicenseConcluded { get; set; }

        [JsonProperty("licenseInfoInFiles")]
        public List<string> LicenseInfoInFiles { get; set; }

        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }
    }

    public class Package
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        public string SPDXID { get; set; }

        [JsonProperty("versionInfo")]
        public string VersionInfo { get; set; }

        [JsonProperty("supplier")]
        public string Supplier { get; set; }

        [JsonProperty("downloadLocation")]
        public string DownloadLocation { get; set; }

        [JsonProperty("filesAnalyzed")]
        public bool FilesAnalyzed { get; set; }

        [JsonProperty("checksums")]
        public List<Checksum> Checksums { get; set; }

        [JsonProperty("sourceInfo")]
        public string SourceInfo { get; set; }

        [JsonProperty("licenseConcluded")]
        public string LicenseConcluded { get; set; }

        [JsonProperty("licenseDeclared")]
        public string LicenseDeclared { get; set; }

        [JsonProperty("copyrightText")]
        public string CopyrightText { get; set; }

        [JsonProperty("externalRefs")]
        public List<ExternalRef> ExternalRefs { get; set; }
    }

    public class Relationship
    {
        [JsonProperty("spdxElementId")]
        public string SpdxElementId { get; set; }

        [JsonProperty("relatedSpdxElement")]
        public string RelatedSpdxElement { get; set; }

        [JsonProperty("relationshipType")]
        public string RelationshipType { get; set; }
    }
}
