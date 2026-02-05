// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;

namespace LCT.Common
{
    /// <summary>
    /// CycloneDX BomParser interface
    /// </summary>
    public interface ICycloneDXBomParser
    {
        /// <summary>
        /// Parses a CycloneDX BOM file and returns a Bom object.
        /// </summary>
        /// <param name="filePath">The path to the CycloneDX BOM file.</param>
        /// <returns>The parsed Bom object.</returns>
        public Bom ParseCycloneDXBom(string filePath);

    }
}
