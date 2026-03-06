using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LCT.SBOMSigningVerification.Model
{
    public class Signature
    {
        [JsonPropertyName("algorithm")]
        public string? Algorithm { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
