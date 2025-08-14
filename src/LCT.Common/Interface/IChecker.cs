// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.ComplianceValidator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.Common.Interface
{
    public interface IChecker : IPrintRecommendation, IPrintWarning
    {
        Task<ComplianceSettingsModel> LoadSettingsAsync(string filePath);

        bool Check(ComplianceSettingsModel settings, Object input);

        List<string> GetResults();
    }
}
