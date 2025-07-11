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
        public Bom ParseSPDXBom(string filePath);
    }
}
