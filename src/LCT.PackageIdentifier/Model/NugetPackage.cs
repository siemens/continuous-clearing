// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Nuget Package
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class NugetPackage
    {
        public string ID { get; set; }

        public string Version { get; set; }
        public List<string> Dependencies { get; set; }

        public string Filepath { get; set; }
        public string IsDev { get; set; }

    }
}
