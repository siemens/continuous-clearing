// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents components that require action based on various conditions.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ComponentsRequireAction
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of components without source attachment.
        /// </summary>
        public List<ComparisonBomData> ListofComponentsWithoutSourceAttachment { get; set; } = new List<ComparisonBomData>();

        /// <summary>
        /// Gets or sets the list of components without source download URL.
        /// </summary>
        public List<Components> ListofComponentsWithoutSrcDownloadUrl { get; set; } = new List<Components>();

        /// <summary>
        /// Gets or sets the list of components not uploaded.
        /// </summary>
        public List<Components> ListofComponentsNotUploaded { get; set; } = new List<Components>();

        #endregion
    }
}
