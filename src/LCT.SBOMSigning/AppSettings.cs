// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using SBOMSigning.Enum;

namespace LCT.SBOMSigning
{
    public class AppSettings
    {
        private OperationType ennumOp;
        public string BomFilePath { get; set; }
        public string LogFolderPath { get; set; }
        public string KeyVaultURI { get; set; }
        public string CertificateName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string OutputFolderPath { get; set; }
        public OperationType Operation { get; set; }

        public bool UseLocalCertificate { get; set; }
        public string LocalCertificatePath { get; set; }
        public string LocalCertificatePassword { get; set; }



    }
}
