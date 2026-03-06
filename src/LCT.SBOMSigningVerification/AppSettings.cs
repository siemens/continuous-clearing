using LCT.SBOMSigningVerification.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SBOMSigningVerification
{
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
