// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Tommy;

namespace LCT.Common.Interface
{
    internal interface IFileParser
    {
        public TomlTable ParseTomlFile(string filePath);
    }
}
