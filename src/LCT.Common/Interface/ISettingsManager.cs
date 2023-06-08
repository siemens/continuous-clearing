// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Interface
{
    /// <summary>
    /// ISettingsManager interface
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Reads the Configuration from input args and json setting file
        /// </summary>
        /// <param name="args">args</param>
        /// <param name="jsonSettingsFileName">jsonSettingsFileName</param>
        /// <returns>AppSettings</returns>
        public T ReadConfiguration<T>(string[] args, string jsonSettingsFileName);
    }
}
