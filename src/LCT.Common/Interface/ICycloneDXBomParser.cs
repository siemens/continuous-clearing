// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        public Bom ParseCycloneDXBom(string filePath);
    }
}
