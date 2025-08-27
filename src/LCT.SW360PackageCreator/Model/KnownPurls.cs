using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Model
{
    public class KnownPurls
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty("knownPurls")]
        public List<KnownPurl> KnownPurlList { get; set; }
    }

    public class KnownPurl
    {
        [JsonProperty("sw360Name")]
        public string Sw360Name { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("vcs")]
        public string Vcs { get; set; }

        [JsonProperty("purls")]
        public List<string> Purls { get; set; }
    }
}
