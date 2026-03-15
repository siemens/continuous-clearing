// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    [ExcludeFromCodeCoverage]
    public class SbomSigningConfig
    {

        public string KeyVaultURI { get; set; }
        public string CertificateName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public bool SBOMSignVerify { get; set; } = true;       

    }
}
