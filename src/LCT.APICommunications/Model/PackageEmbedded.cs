using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class PackageEmbedded
    {
        [JsonProperty("sw360:packages")]
        public IList<Sw360Packages> Sw360packages { get; set; }
    }
}
