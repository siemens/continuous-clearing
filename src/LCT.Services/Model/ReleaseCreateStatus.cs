// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Services.Model
{
    /// <summary>
    /// ReleaseCreateStatus model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ReleaseCreateStatus
    {
        public bool IsCreated { get; set; }

        public string ReleaseIdToLink { get; set; }

        public string AttachmentApiUrl { get; set; }
    }
}
