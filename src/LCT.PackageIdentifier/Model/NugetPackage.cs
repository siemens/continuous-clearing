// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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

    public string Filepath { get; set; }
    public string IsDev { get; set; }

    }
}
