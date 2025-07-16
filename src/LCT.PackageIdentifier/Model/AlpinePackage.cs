// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace LCT.PackageIdentifier.Model
{

    public class AlpinePackage
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string PurlID { get; set; }
        public bool SpdxComponent { get; set; }=false;
        public string SpdxFilePath { get; set; }
    }
}