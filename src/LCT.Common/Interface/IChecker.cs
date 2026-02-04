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
        /// <summary>
        /// Asynchronously loads compliance settings from the specified JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON file containing compliance settings.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded ComplianceSettingsModel.</returns>
        Task<ComplianceSettingsModel> LoadSettingsAsync(string jsonFilePath);

        /// <summary>
        /// Checks the provided data against the specified compliance settings.
        /// </summary>
        /// <param name="settings">The compliance settings to use for validation.</param>
        /// <param name="data">The data to check for compliance.</param>
        /// <returns>True if the data is compliant; otherwise, false.</returns>
        bool Check(ComplianceSettingsModel settings, Object data);

        /// <summary>
        /// Gets the results of the compliance checks.
        /// </summary>
        /// <returns>A list of result strings.</returns>
        List<string> GetResults();
    }
}
