// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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
