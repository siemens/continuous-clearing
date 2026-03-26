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
        /// <param name="args">The command-line arguments.</param>
        /// <param name="jsonSettingsFileName">The name of the JSON settings file.</param>
        /// <param name="environmentHelper">The environment helper for system interactions.</param>
        /// <returns>An instance of the application settings type T.</returns>
        public T ReadConfiguration<T>(string[] args, string jsonSettingsFileName, IEnvironmentHelper environmentHelper) where T : class;

        /// <summary>
        /// Checks that all required arguments are present to run the application.
        /// </summary>
        /// <param name="appSettings">The application settings object.</param>
        /// <param name="currentExe">The name of the current executable.</param>
        public void CheckRequiredArgsToRun(CommonAppSettings appSettings, string currentExe);
    }
}
