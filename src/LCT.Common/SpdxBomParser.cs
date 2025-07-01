// Ignore Spelling: LCT Spdx

using CycloneDX.Models;
using LCT.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common
{
    public class SpdxBomParser : ISpdxBomParser
    {
        public Bom ParseSPDXBom(string filePath)
        {
            Bom bom = new Bom();
            return bom;
        }
    }
}
