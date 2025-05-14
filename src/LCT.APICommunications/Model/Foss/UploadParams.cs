// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The fossology upload package params model
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadParams
    {
        public string FolderId { get; set; }

        public string UploadDescription { get; set; }

        public string Public { get; set; }
    }
}
