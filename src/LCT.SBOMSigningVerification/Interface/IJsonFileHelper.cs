using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SBOMSigningVerification.Interface
{
    public interface IJsonFileHelper
    {
        string SignSBOMFile();
        public void ReadSBOMFile(string sbomFilePath, out bool isValid);
    }
}
