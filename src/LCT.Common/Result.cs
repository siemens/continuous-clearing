// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common
{
    /// <summary>
    /// Run process result
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Result
    {
        /// <summary>
        /// Exit code
        /// <para>If NULL, process exited due to timeout</para>
        /// </summary>
        public int? ExitCode { get; set; } = null;

        /// <summary>
        /// Standard error stream
        /// </summary>
        public string StdErr { get; set; } = "";

        /// <summary>
        /// Standard output stream
        /// </summary>
        public string StdOut { get; set; } = "";
    }
}
