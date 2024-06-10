// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// MultipleVersionValues model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MultipleVersionValues
    {
        public string ComponentName { get; set; }
        public string ComponentVersion { get; set; }
        public string PackageFoundIn { get; set; }

    }

    public class MultipleVersions
    {
        public List<MultipleVersionValues> Npm { get; set; }
        public List<MultipleVersionValues> Nuget { get; set; }
        public List<MultipleVersionValues> Conan { get; set; }
    }
}
