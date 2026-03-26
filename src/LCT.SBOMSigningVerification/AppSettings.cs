// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.SBOMSigningVerification.Enum;

namespace LCT.SBOMSigningVerification
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AppSettings
    {
        public string? BomFilePath { get; set; }
        public string? KeyVaultURI { get; set; }
        public string? CertificateName { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? bomcontent { get; set; }
        public OperationType Operation { get; set; }

        public bool? SBOMSignVerify { get; set; }
        

    }
}
