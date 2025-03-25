// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadArgs
    {
        public string PackageName { get; set; }
        public string ReleaseName { get; set; }
        public string Version { get; set; }
    }
}
