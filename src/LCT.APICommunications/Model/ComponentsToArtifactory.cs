// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsToArtifactory
    {
        public string Name { get; set; }
        public string PackageName { get; set; }
        public string Version { get; set; }
        public string ComponentType { get; set; }
        public string JfrogApi { get; set; }
        public string SrcRepoName { get; set; }
        public string SrcRepoPathWithFullName { get; set; }
        public string PypiCompName { get; set; }
        public string DestRepoName { get; set; }
        public string ApiKey { get; set; }
        public string Email { get; set; }
        public string PackageInfoApiUrl { get; set; }
        public string CopyPackageApiUrl { get; set; }
        public string MovePackageApiUrl { get; set; }
        public string PackageExtension { get; set; }
        public string Path { get; set; }
        public  PackageType PackageType { get; set; }
        public bool DryRun { get; set; } = true;
        public string Purl { get; set; }
        public string JfrogPackageName { get;set; }
    }
}
