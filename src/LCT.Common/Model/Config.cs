// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Config
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Config
    {
        public string[] Include { get; set; }
        public string[] Exclude { get; set; }
        public Artifactory Artifactory { get; set; }
        public string ReleaseRepo { get; set; }
        public string DevDepRepo { get; set; }

    }
    public class Artifactory
    {
        public List<ThirdPartyRepo> ThirdPartyRepos { get; set; }
        public string[] InternalRepos { get; set; } = Array.Empty<string>();
        public string[] DevRepos { get; set; }
        public string[] RemoteRepos { get; set; }
    }
    public class ThirdPartyRepo
    {
        public string Name { get; set; }
        public bool Upload { get; set; }
    }
}
