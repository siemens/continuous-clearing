// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Spdx Bom SIT

using CycloneDX.Models;

namespace SIT.Common.Interface
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
