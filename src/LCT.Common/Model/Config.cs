﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

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
        public List<string> ExcludedComponents { get; set; }
        public string[] JfrogNpmRepoList { get; set; }
        public string[] JfrogNugetRepoList { get; set; }
        public string[] JfrogMavenRepoList { get; set; }
        public string[] DevDependentScopeList { get; set; }
        
    }
}
