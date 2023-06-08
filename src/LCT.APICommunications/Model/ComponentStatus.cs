// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public  class ComponentStatus
    {
        public Sw360Components Sw360components { get; set; }
        public  bool isComponentExist { get; set; }
    }
}
