// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using log4net;
using PackageUrl;
using System;
using System.Reflection;

namespace SIT.Common
{
    /// <summary>
    /// Provides a shared, ecosystem-aware helper for normalizing package names and
    /// validating that they form a well-formed Package URL (purl). This centralizes the
    /// logic previously duplicated in individual processors (e.g. PyPI normalization).
    /// </summary>
    /// <remarks>
    /// Normalization rules are driven by the Package URL (purl) specification:
    /// https://github.com/package-url/purl-spec/blob/master/PURL-TYPES.rst
    ///
    /// Per-ecosystem name handling:
    ///  - pypi : name is lowercased (see also PEP 503 https://peps.python.org/pep-0503/#normalized-names).
    ///  - npm  : name is lowercased (published packages are lowercase).
    ///  - nuget: case-insensitive but case-preserving in the purl (NOT normalized here).
    ///  - cargo, conan, deb (Debian), apk (Alpine): names are used as-is (no lowercasing).
    ///
    /// Because only pypi and npm require lowercasing, this helper always lowercases and is
    /// intended to be called only for those ecosystems. Other project types build their
    /// purl directly from the name provided by the input file.
    /// </remarks>
    public static class PurlNameNormalizer
    {
        #region Fields
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Methods
        /// <summary>
        /// Normalizes a package name to its canonical lowercase form for the given purl
        /// type and validates that the resulting name forms a well-formed purl. If the
        /// purl cannot be constructed, a warning is logged and the lowercased name is
        /// returned unchanged.
        /// </summary>
        /// <param name="name">The package name to normalize.</param>
        /// <param name="purlType">The purl type (e.g. "pypi", "npm").</param>
        /// <returns>The normalized (lowercase) package name.</returns>
        public static string Normalize(string name, string purlType)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            string normalizedName = name.ToLowerInvariant();

            try
            {
                // Validate that the normalized name forms a well-formed purl.
                _ = new PackageURL(purlType, null, normalizedName, null, null, null);
            }
            catch (MalformedPackageUrlException ex)
            {
                Logger.WarnFormat("PurlNameNormalizer.Normalize(): Unable to validate purl for package name '{0}' (type '{1}'). Using original name. Error: {2}", name, purlType, ex.Message);
            }

            return normalizedName;
        }
        #endregion
    }
}
