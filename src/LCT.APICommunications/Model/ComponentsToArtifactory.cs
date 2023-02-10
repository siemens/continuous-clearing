// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    public class ComponentsToArtifactory
    {
        public string Name { get; set; }
        public string PackageName { get; set; }
        public string Version { get; set; }
        public string ComponentType { get; set; }
        public string JfrogApi { get; set; }
        public string SrcRepoName { get; set; }
        public string DestRepoName { get; set; }
        public string ApiKey { get; set; }
        public string Email { get; set; }
        public string PackageInfoApiUrl { get; set; }
        public string CopyPackageApiUrl { get; set; }
        public string PackageExtension { get; set; }

    }
}
