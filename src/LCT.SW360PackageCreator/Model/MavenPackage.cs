// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.SW360PackageCreator.Model
{

    public class MavenPackage
    {
        public string ID { get; set; }

        public string Version { get; set; }
        public string GroupID { get; }
    }
}
