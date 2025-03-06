using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.Model
{
    public class Sw360Packages
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("purl")]
        public string Purl { get; set; }
    }
}
