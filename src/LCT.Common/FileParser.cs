﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common.Interface;
using System;
using System.IO;
using Tommy;

namespace LCT.Common
{
    public class FileParser : IFileParser
    {
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
            catch(Exception ex)
            {
                return new TomlTable();
            }
        }
    }
}