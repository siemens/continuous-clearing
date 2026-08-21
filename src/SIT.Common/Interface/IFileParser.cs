// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Tommy;

namespace SIT.Common.Interface
{
    internal interface IFileParser
    {
        /// <summary>
        /// Parses a TOML file and returns its contents as a TomlTable.
        /// </summary>
        /// <param name="filePath">The path to the TOML file.</param>
        /// <returns>The parsed TOML table.</returns>
        public TomlTable ParseTomlFile(string filePath);
    }
}
