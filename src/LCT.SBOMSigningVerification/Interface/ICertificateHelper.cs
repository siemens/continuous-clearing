using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SBOMSigningVerification.Interface
{
    public interface ICertificateHelper
    {
        byte[] SignCertificate(string content);
        bool VerifySignature(string content, string signature);
    }
}
