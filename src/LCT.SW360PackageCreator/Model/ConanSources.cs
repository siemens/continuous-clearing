// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace LCT.SW360PackageCreator.Model
{
    [ExcludeFromCodeCoverage]
    public class Sources
    {
        [YamlMember(Alias ="sources")]
        public Dictionary<string, Source> SourcesData { get; set; }
        [YamlMember(Alias = "patches")]
        public Dictionary<string, List<Patch>> Patches { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Source
    {
        [YamlMember(Alias = "url")]
        public object Url { get; set; }
        [YamlMember(Alias = "sha256")]
        public string Sha256 { get; set; }


    }

    [ExcludeFromCodeCoverage]
    public class Patch
    {
        public string PatchFile { get; set; }
        public string PatchDescription { get; set; }
        public string PatchType { get; set; }
        public string PatchSource { get; set; }
        public string Sha256 { get; set; }
    }
}
