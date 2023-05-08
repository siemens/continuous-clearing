// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public  class ComponentStatus
    {
        public Sw360Components Sw360components { get; set; }
        public  bool isComponentExist { get; set; }
    }
}
