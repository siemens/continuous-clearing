// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Services.Model
{
    /// <summary>
    /// ComponentCreateStatus 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ComponentCreateStatus
    {
        public bool IsCreated { get; set; }
        public ReleaseCreateStatus ReleaseStatus { get; set; }
    }
}
