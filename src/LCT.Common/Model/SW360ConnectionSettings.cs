// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
// SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SW360ConnectionSettings
    {
        public string SW360URL { get; set; }
        public string SW360AuthTokenType { get; set; }
        public string Sw360Token { get; set; }
        public bool IsTestMode { get; set; }
        public int Timeout { get; set; }

    }
}
