using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class PackageLinked
    {
        public string PackageName { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string PackageId { get; set; } = string.Empty;
        public string Packagelink { get; set; } = string.Empty;
    }
}
