// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Represents Conan sources and patches data.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Sources
    {
        #region Properties
        /// <summary>
        /// Gets or sets the dictionary of sources data.
        /// </summary>
        [YamlMember(Alias = "sources")]
        public Dictionary<string, Source> SourcesData { get; set; }
        
        /// <summary>
        /// Gets or sets the dictionary of patches.
        /// </summary>
        [YamlMember(Alias = "patches")]
        public Dictionary<string, List<Patch>> Patches { get; set; }
        #endregion
    }

    /// <summary>
    /// Represents a Conan source with URL and SHA256 hash.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Source
    {
        #region Properties
        /// <summary>
        /// Gets or sets the URL of the source.
        /// </summary>
        [YamlMember(Alias = "url")]
        public object Url { get; set; }
        
        /// <summary>
        /// Gets or sets the SHA256 hash of the source.
        /// </summary>
        [YamlMember(Alias = "sha256")]
        public string Sha256 { get; set; }
        #endregion
    }

    /// <summary>
    /// Represents a patch with file, description, type, source, and SHA256 hash.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Patch
    {
        #region Properties
        /// <summary>
        /// Gets or sets the patch file name.
        /// </summary>
        public string PatchFile { get; set; }
        
        /// <summary>
        /// Gets or sets the patch description.
        /// </summary>
        public string PatchDescription { get; set; }
        
        /// <summary>
        /// Gets or sets the patch type.
        /// </summary>
        public string PatchType { get; set; }
        
        /// <summary>
        /// Gets or sets the patch source.
        /// </summary>
        public string PatchSource { get; set; }
        
        /// <summary>
        /// Gets or sets the SHA256 hash of the patch.
        /// </summary>
        public string Sha256 { get; set; }
        #endregion
    }
}
