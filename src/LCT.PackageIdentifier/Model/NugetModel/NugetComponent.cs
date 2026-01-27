// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetComponent"/> class with the specified id and version.
        /// </summary>
        /// <param name="id">Component name or identifier.</param>
        /// <param name="version">Component version.</param>
        public NuGetComponent(string id, string version) : base(id, version)
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Package URL (purl) for this NuGet component.
        /// </summary>
        /// <returns>String representation of the package URL for the NuGet component.</returns>
        public override string PackageUrl => new PackageURL("nuget", null, Name, Version, null, null).ToString();
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
