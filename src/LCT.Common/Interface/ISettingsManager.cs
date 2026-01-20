// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        /// Reads the configuration from input arguments and a JSON settings file.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="jsonSettingsFileName">The name of the JSON settings file.</param>
        /// <returns>The deserialized configuration object of type T.</returns>
        public T ReadConfiguration<T>(string[] args, string jsonSettingsFileName);

        /// <summary>
        /// Checks that all required arguments are present to run the application.
        /// </summary>
        /// <param name="appSettings">The application settings object.</param>
        /// <param name="currentExe">The name of the current executable.</param>
        /// <returns>void.</returns>
        public void CheckRequiredArgsToRun(CommonAppSettings appSettings, string currentExe);
    }
}
