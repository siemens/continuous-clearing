// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------
using System;
using Newtonsoft.Json.Linq;

namespace LCT.PackageIdentifier
{
    public class LicenseInfoExtractor
    {
        /// <summary>
        /// Extracts license information from package metadata for supported package types.
        /// </summary>
        /// <param name="packageType">Type of the package (e.g., "npm", "nuget")</param>
        /// <param name="metadata">Package metadata as JObject</param>
        /// <returns>License string or "Not Found"</returns>
        public string ExtractLicense(string packageType, JObject metadata)
        {
            if (metadata == null)
                return "Not Found";

            switch (packageType.ToLower())
            {
                case "npm":
                    // Try standard license field
                    if (metadata["license"] != null)
                        return metadata["license"].ToString();
                    // Try licenses array
                    if (metadata["licenses"] != null)
                        return string.Join(", ", metadata["licenses"]);
                    break;

                case "nuget":
                    if (metadata["LicenseUrl"] != null && !string.IsNullOrEmpty(metadata["LicenseUrl"].ToString()))
                        return metadata["LicenseUrl"].ToString();
                    if (metadata["LicenseExpression"] != null)
                        return metadata["LicenseExpression"].ToString();
                    break;

                // Add more cases for other package types as needed

                default:
                    break;
            }
            return "Not Found";
        }
    }
}
