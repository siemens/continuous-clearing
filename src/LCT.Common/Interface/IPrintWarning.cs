// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Interface
{
    public interface IPrintWarning
    {
        /// <summary>
        /// Prints a warning message to the output.
        /// </summary>
        /// <param name="content">The warning message to be displayed. Cannot be null or empty.</param>
        void PrintWarning(string content);
    }
}
