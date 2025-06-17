// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Net.Http;

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
        public string PypiOrNpmCompName { get; set; }
        public string DestRepoName { get; set; }
        public string Token { get; set; }
        public string PackageInfoApiUrl { get; set; }
        public string CopyPackageApiUrl { get; set; }
        public string MovePackageApiUrl { get; set; }
        public string PackageExtension { get; set; }
        public string Path { get; set; }
        public PackageType PackageType { get; set; }
        public bool DryRun { get; set; } = true;
        public string Purl { get; set; }
        public string JfrogPackageName { get; set; }
        public string OperationType { get; set; }
        public string DryRunSuffix { get; set; }
        public string JfrogRepoPath { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
    }
}
