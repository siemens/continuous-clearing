// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    [ExcludeFromCodeCoverage]
    public class ComponentsRequireAction
    {
        public List<ComparisonBomData> ListofComponentsWithoutSourceAttachment { get; set; } = new List<ComparisonBomData>();

        public List<Components> ListofComponentsWithoutSrcDownloadUrl { get; set; } = new List<Components>();

        public List<Components> ListofComponentsNotUploaded { get; set; } = new List<Components>();
    }
}
