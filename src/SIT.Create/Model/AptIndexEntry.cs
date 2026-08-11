// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SIT.Create.Model
{
    /// <summary>
    /// A stanza of the "Sources" index of an APT archive.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptSourceEntry
    {
        /// <summary>
        /// Gets or sets the version of the source package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the pool folder the source files are stored in, relative to the archive root.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets or sets the file names that belong to the source package.
        /// </summary>
        public List<string> FileNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// A stanza of the "Packages" index of an APT archive.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptBinaryEntry
    {
        /// <summary>
        /// Gets or sets the version of the binary package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the source package the binary package was built from.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Gets or sets the version of the source package the binary package was built from.
        /// </summary>
        public string SourceVersion { get; set; }

        /// <summary>
        /// Gets or sets the path of the .deb inside the pool, relative to the archive root.
        /// </summary>
        public string FileName { get; set; }
    }

    /// <summary>
    /// The parsed "Sources" index of one component of a suite.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptSourceIndex
    {
        /// <summary>
        /// Gets the source packages of the component, by package name.
        /// </summary>
        public Dictionary<string, List<AptSourceEntry>> Packages { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the pool folder of every source file of the component, by file name.
        /// </summary>
        public Dictionary<string, string> FileLocations { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// One component of one suite of one APT archive, i.e. the scope of a pair of index files.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptIndexContext
    {
        /// <summary>
        /// Gets or sets the configured name of the repository, used for logging only.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets the archive root, i.e. the URL that contains the "dists" and "pool" folders.
        /// </summary>
        public string ArchiveRoot { get; set; }

        /// <summary>
        /// Gets or sets the suite (codename).
        /// </summary>
        public string Suite { get; set; }

        /// <summary>
        /// Gets or sets the archive component.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the binary architectures of the suite.
        /// </summary>
        public string[] Architectures { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// The place a source package and its files were found at.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptSourceLocation
    {
        /// <summary>
        /// Gets or sets the index the source package was found through.
        /// </summary>
        public AptIndexContext Context { get; set; }

        /// <summary>
        /// Gets or sets the name of the source package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the source package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the pool folder of the source package, relative to the archive root.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets or sets the file names that belong to the source package.
        /// </summary>
        public List<string> FileNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// The parts of the "Release" file of a suite that are needed to locate its index files.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptReleaseInfo
    {
        /// <summary>
        /// Gets or sets the archive components announced by the suite.
        /// </summary>
        public string[] Components { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the architectures announced by the suite.
        /// </summary>
        public string[] Architectures { get; set; } = Array.Empty<string>();
    }
}
