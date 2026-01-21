// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// CycloneDx Data
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class CycloneDxBomData
    {
        #region Properties
        /// <summary>
        /// Format of the BOM (e.g. "CycloneDX").
        /// </summary>
        [JsonProperty("bomFormat")]
        public string BomFormat { get; set; }

        /// <summary>
        /// Spec version used by the CycloneDX BOM.
        /// </summary>
        [JsonProperty("specVersion")]
        public string SpecVersion { get; set; }

        /// <summary>
        /// Array of component information entries contained in the BOM.
        /// </summary>
        [JsonProperty("components")]
        public ComponentsInfo[] ComponentsInfo { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
