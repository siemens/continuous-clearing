// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static LCT.SW360PackageCreator.Model.ReleasesAllDetails;

namespace LCT.SW360PackageCreator.Model
{
    public class ReleasesAllDetails
    {
        [ExcludeFromCodeCoverage]
        [JsonProperty("_embedded")]
        public AllReleasesEmbedded Embedded { get; set; }
        [JsonProperty("page")]
        public Pagination Page { get; set; }
        public class AllReleasesEmbedded
        {
            [JsonProperty("sw360:releases")]
            public List<Sw360Release> Sw360releases { get; set; }

            [JsonProperty("sw360:attachments")]
            public List<List<Attachment>> Sw360attachments { get; set; }

        }
        public class Attachment
        {
            [JsonProperty("filename")]
            public string Filename { get; set; }

        }
        public class Links
        {
            [JsonProperty("self")]
            public Self Self { get; set; }

        }       
        public class Self
        {
            [JsonProperty("href")]
            public string Href { get; set; }
        }
        public class Sw360Release
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("version")]
            public string Version { get; set; }
            [JsonProperty("clearingState")]
            public string ClearingState { get; set; }
            [JsonProperty("_links")]
            public Links Links { get; set; }
            [JsonProperty("_embedded")]
            public AllReleasesEmbedded AllReleasesEmbedded { get; set; }

        }
        public class Pagination
        {
            [JsonProperty("totalPages")]
            public int TotalPages { get; set; }

        }
    }
}
