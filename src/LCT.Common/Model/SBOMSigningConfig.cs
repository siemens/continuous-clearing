using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Model
{
    public class SbomSigningConfig
    {

        public string LogFolderPath { get; set; }
        public string KeyVaultURI { get; set; }
        public string CertificateName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string OutputFolderPath { get; set; }
        public bool EnableSigning { get; set; }
        public bool SBOMSignVerify { get; set; }
        public bool EnableValidation { get; set; }
        
    }
}
