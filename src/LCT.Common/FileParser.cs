// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common.Interface;
using System.IO;
using Tommy;

namespace LCT.Common
{
    public class FileParser : IFileParser
    {
        #region Methods

        /// <summary>
        /// Parses a TOML file and returns the parsed table.
        /// </summary>
        /// <param name="filePath">The file path of the TOML file to parse.</param>
        /// <returns>A TomlTable containing the parsed data, or an empty TomlTable if parsing fails.</returns>
        public TomlTable ParseTomlFile(string filePath)
        {
            TomlTable table;
            try
            {
                using StreamReader reader = File.OpenText(filePath);
                // Parsing the table
                table = TOML.Parse(reader);
                return table;
            }
            catch (TomlParseException)
            {
                return new TomlTable();
            }
        }

        #endregion
    }
}
