// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Spdx Bom LCT

using CycloneDX.Models;

namespace LCT.Common.Interface
{
    public interface ISpdxBomParser
    {
        /// <summary>
        /// Parses an SPDX BOM file and returns a Bom object.
        /// </summary>
        /// <param name="filePath">The path to the SPDX BOM file.</param>
        /// <returns>The parsed Bom object.</returns>
        public Bom ParseSPDXBom(string filePath);
    }
}
