// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace SIT.Common
{
    /// <summary>
    /// Helper for version regex checks used across projects.
    /// </summary>
    public static partial class RegexHelper
    {
        [GeneratedRegex(@"(?nx)^
(?<Major>0|[1-9]\d*)\.
(?<Minor>0|[1-9]\d*)\.
(?<Patch>0|[1-9]\d*)
(?<PreReleaseTagWithSeparator>
  -(?<PreReleaseTag>
    ((0|[1-9]\d*|\d*[A-Z-a-z-][\dA-Za-z-]*))(\.(0|[1-9]\d*|\d*[A-Za-z-][\dA-Za-z-]*))*
   )
)?
(?<BuildMetadataTagWithSeparator>
  \+(?<BuildMetadataTag>[\dA-Za-z-]+(\.[\dA-Za-z-]+)*)
)?$")]
        private static partial Regex PreReleaseVerRegEx();

        [GeneratedRegex(@"^(hf|HF|sp|SP)\d*\.\d+$")]
        private static partial Regex MaintenanceRegEx();

        /// <summary>
        /// Returns true if the given version should be considered a release version.
        /// - returns true when there is no pre-release part
        /// - returns true when the pre-release part is a maintenance tag (hf/HF/sp/SP)
        /// - otherwise returns false (pre-release)
        /// </summary>
        public static bool IsReleaseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            Match match = PreReleaseVerRegEx().Match(version);

            if (!match.Success)
                return false;

            Group preReleaseGroup = match.Groups["PreReleaseTag"];

            // If there is no pre-release part => it's a release
            if (!preReleaseGroup.Success)
            {
                return true;
            }

            // If pre-release part matches maintenance tag (hf/HF/sp/SP), treat as release
            Match maintenanceMatch = MaintenanceRegEx().Match(preReleaseGroup.Value);
            if (maintenanceMatch.Success)
            {
                return true;
            }

            // Any other pre-release -> NOT a release
            return false;
        }
    }
}
