// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using System.Collections.Generic;

namespace LCT.Common
{
    /// <summary>
    /// CycloneDX BomParser interface
    /// </summary>
    public interface ICycloneDXBomParser
    {
        public List<Component> ParseCycloneDXBom(string filePath);
    }
}
