// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Configuration for SBOM signing operations
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SbomSigningConfig
    {

        public string LogFolderPath { get; set; }
        public string KeyVaultURI { get; set; }
        public string CertificateName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public bool IsSignVerifyRequired { get; set; }
        
    }
}