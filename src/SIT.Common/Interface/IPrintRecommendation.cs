// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace SIT.Common.Interface
{
    public interface IPrintRecommendation
    {
        /// <summary>
        /// Prints a specific recommendation message.
        /// </summary>
        /// <param name="content">The content of the recommendation message to be printed.</param>
        /// <returns>void.</returns>
        void PrintRecommendation(string content);
    }
}
