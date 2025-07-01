using CycloneDX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Interface
{
    public interface ISpdxBomParser
    {
        public Bom ParseSPDXBom(string filePath);
    }
}
