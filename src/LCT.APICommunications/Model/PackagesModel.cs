using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.Model
{
    public class PackagesModel
    {
        [JsonProperty("_embedded")]
        public PackageEmbedded Embedded { get; set; }
    }
}
