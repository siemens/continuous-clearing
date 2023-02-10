// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Text;

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
