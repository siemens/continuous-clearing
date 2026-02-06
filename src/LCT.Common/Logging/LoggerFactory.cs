// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using log4net;
using System.Reflection;

namespace LCT.Common
{
    public static class LoggerFactory
    {
        #region Fields
        // No fields present.
        #endregion

        #region Properties
        /// <summary>
        /// Determines whether to use Spectre.Console for logging or log4net.
        /// </summary>
        public static bool UseSpectreConsole { get; set; } = true;
        #endregion

        #region Constructors
        // No constructors present.
        #endregion

        #region Methods
        /// <summary>
        /// Gets a logger for the specified type.
        /// </summary>
        /// <param name="type">The type for which to get the logger.</param>
        /// <returns>An ILog instance for the specified type.</returns>
        public static ILog GetLogger(System.Type type)
        {
            if (UseSpectreConsole)
                return new SpectreLogAdapter(type.FullName);
            else
                return LogManager.GetLogger(type);
        }

        /// <summary>
        /// Gets a logger for the specified method.
        /// </summary>
        /// <param name="method">The method for which to get the logger.</param>
        /// <returns>An ILog instance for the method's declaring type.</returns>
        public static ILog GetLogger(MethodBase method)
        {
            if (UseSpectreConsole)
                return new SpectreLogAdapter(method.DeclaringType?.FullName);
            else
                return LogManager.GetLogger(method.DeclaringType);
        }
        #endregion

        #region Events
        // No events present.
        #endregion
    }
}