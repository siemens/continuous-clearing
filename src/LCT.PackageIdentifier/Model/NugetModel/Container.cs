// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model.NugetModel
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ContainerType
    {
        unknown = 0,
        nuget = 1
    }


    /// <summary>
    /// A container is a logical group which uses packages.
    /// E.g., this could be a <c>nuget.config</c> file or a <c>*.csproj</c> file.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Container
    {
        #region Properties
        /// <summary>
        /// Components contained in this container, keyed by component identifier.
        /// </summary>
        public IDictionary<string, BuildInfoComponent> Components { get; set; } = new Dictionary<string, BuildInfoComponent>();

        /// <summary>
        /// Name of the container (for example a file name or logical group name).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the container (for example nuget).
        /// </summary>
        public ContainerType Type { get; set; }

        /// <summary>
        /// Default scope applied to components in this container.
        /// </summary>
        public ComponentScope Scope { get; set; } = ComponentScope.Required;
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
