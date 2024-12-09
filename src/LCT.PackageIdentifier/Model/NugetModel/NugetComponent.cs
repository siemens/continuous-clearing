// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using PackageUrl;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model.NugetModel
{
    [ExcludeFromCodeCoverage]
    public class NuGetComponent : BuildInfoComponent
    {
        public NuGetComponent(string id, string version) : base(id, version)
        {
        }

        public override string PackageUrl => new PackageURL("nuget", null, Name, Version, null, null).ToString();
    }
}
