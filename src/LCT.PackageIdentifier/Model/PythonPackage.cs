
// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class PythonPackage
    {
        public string PurlID { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public bool Isdevdependent { get; set; }
        public string FoundType { get; set; }
        public string Filepath { get; set; }

    }
}
