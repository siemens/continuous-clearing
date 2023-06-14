// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using log4net;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LCT.Common
{
    /// <summary>
    /// The SettingsManager class
    /// </summary>
    public class SettingsManager : ISettingsManager
    {
        public string BasePath { get; private set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Reads the Configuration from input args and json setting file
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>AppSettings</returns>
        public T ReadConfiguration<T>(string[] args, string jsonSettingsFileName)
        {
            Logger.Debug($"ReadConfiguration():Start");

            if (args != null)
            {
                Logger.Debug($"ReadConfiguration():args: {string.Join(",", args)}");
            }
            if (args?.Length == 0)
            {
                Logger.Debug($"Argument Count : {args.Length}");
                DisplayHelp();
                Environment.Exit(0);
            }
            string settingsFilePath = GetConfigFilePathFromArgs(args, jsonSettingsFileName);
            Logger.Logger.Log(null, Level.Notice, $"Settings File: {settingsFilePath}", null);

            //add ut for reading - add json and then cmd args
            IConfigurationBuilder settingsConfigBuilder = new ConfigurationBuilder()
                                                                    .SetBasePath(BasePath)
                                                                    .AddJsonFile(settingsFilePath, true, true)
                                                                    .AddEnvironmentVariables()
                                                                    .AddCommandLine(args);


            IConfiguration settingsConfig = settingsConfigBuilder.Build();



            T appSettings = settingsConfig.Get<T>();

            if (appSettings == null)
            {
                Logger.Debug($"ReadConfiguration(): {nameof(appSettings)} is null");

                throw new InvalidDataException(nameof(appSettings));
            }

            Logger.Debug($"ReadConfiguration():End");

            return appSettings;
        }

        private static void DisplayHelp()
        {

            StreamReader sr = new("CLIUsageNpkg.txt");
            //Read the whole file
            string line = sr.ReadToEnd();
            Console.WriteLine(line);

            sr.Dispose();
        }

        internal string GetConfigFilePathFromArgs(string[] args, string jsonSettingsFileName)
        {
            IConfigurationBuilder settingsFileConfigBuilder = new ConfigurationBuilder()
                                                                .AddEnvironmentVariables()
                                                                .AddCommandLine(args);
            IConfiguration settingsFileConfiguration = settingsFileConfigBuilder.Build();

            SettingsFile settings = settingsFileConfiguration.Get<SettingsFile>();

            string settingsFilePath = settings?.SettingsFilePath;
            if (string.IsNullOrWhiteSpace(settings?.SettingsFilePath))
            {
                settingsFilePath = jsonSettingsFileName;
            }


            string defaultFile = Path.Combine(BasePath, settingsFilePath);

            if (!File.Exists(settingsFilePath))
            {
                Logger.Debug($"SettingsFilePath: \"{settingsFilePath}\" does not exist. Using default settings.");
                return defaultFile;
            }

            return settingsFilePath;
        }

    }

    public class SettingsFile
    {
        public string SettingsFilePath { get; set; }
    }
}
