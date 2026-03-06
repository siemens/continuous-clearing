using LCT.SBOMSigningVerification.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SBOMSigningVerification.Interface
{
    public interface ISignatureHelper
    {
        Signature? ExtractSignature(string sbomContent);
        string RemoveSignature(string sbomContent);
    }
}
