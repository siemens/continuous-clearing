// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Interface
{
    public interface IPrintRecommendation
    {
        /// <summary>
        /// Prints a specific recommendation message.
        /// </summary>
        /// param name="content">The content of the recommendation message to be printed.</param>
        void PrintRecommendation(string content);
    }
}
