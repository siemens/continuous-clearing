﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentIdentification
    {
        public List<Component> comparisonBOMData { get; set; }
        public List<Component> internalComponents { get; set; }

    }
}
