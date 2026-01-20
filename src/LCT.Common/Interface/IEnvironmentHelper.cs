// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Interface
{
    public interface IEnvironmentHelper
    {
        /// <summary>
        /// Calls Environment.Exit with the specified exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to use when terminating the process.</param>
        /// <returns>void.</returns>
        void CallEnvironmentExit(int exitCode);

    }
}
