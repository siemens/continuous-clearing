// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using log4net;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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

        public void CheckRequiredArgsToRun(CommonAppSettings appSettings, string currentExe)
        {
            Type type = appSettings.GetType();
            PropertyInfo[] properties = type.GetProperties();

            if (currentExe == "Identifer")
            {
                CheckRequiredArgsToRunPackageIdentifier(appSettings, properties);
            }
            else if (currentExe == "Creator")
            {
                CheckRequiredArgsToRunComponentCreator(appSettings, properties);
            }
            else
            {
                CheckRequiredArgsToRunArtifactoryUploader(appSettings, properties);
            }
        }

        private static void CheckRequiredArgsToRunPackageIdentifier(CommonAppSettings appSettings, PropertyInfo[] properties)
        {
            StringBuilder missingParameters = new StringBuilder();

            //Required parameters to run Package Identifier
            List<string> identifierReqParameters = new List<string>()
            {
                "SW360ProjectID",
                "Sw360Token",
                "SW360URL",
                "JFrogApi",
                "PackageFilePath",
                "BomFolderPath",
                "ArtifactoryUploadApiKey",
                "InternalRepoList",
                "ProjectType"
            };

            foreach (string key in identifierReqParameters)
            {
                string value = properties.First(x => x.Name == key)?.GetValue(appSettings)?.ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    missingParameters.Append(key + "\n");
                }
            }

            if (!string.IsNullOrWhiteSpace(missingParameters.ToString()))
            {
                ExceptionHandling.ArgumentException(missingParameters.ToString());
                Environment.Exit(-1);
            }
        }

        private static void CheckRequiredArgsToRunComponentCreator(CommonAppSettings appSettings, PropertyInfo[] properties)
        {
            StringBuilder missingParameters = new StringBuilder();

            //Required parameters to run SW360Component Creator
            List<string> creatorReqParameters = new List<string>()
            {
                "SW360ProjectID",
                "Sw360Token",
                "SW360URL",
                "BomFilePath"
            };

            foreach (string key in creatorReqParameters)
            {
                string value = properties.First(x => x.Name == key)?.GetValue(appSettings)?.ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    missingParameters.Append(key + " ");
                }
            }

            if (!string.IsNullOrWhiteSpace(missingParameters.ToString()))
            {
                ExceptionHandling.ArgumentException(missingParameters.ToString());
                Environment.Exit(-1);
            }
        }

        private static void CheckRequiredArgsToRunArtifactoryUploader(CommonAppSettings appSettings, PropertyInfo[] properties)
        {
            StringBuilder missingParameters = new StringBuilder();

            //Required parameters to run Artifactory Uploader
            List<string> uploaderReqParameters = new List<string>()
            {
                "JFrogApi",
                "BomFilePath",
                "ArtifactoryUploadApiKey",
            };

            foreach (string key in uploaderReqParameters)
            {
                string value = properties.First(x => x.Name == key)?.GetValue(appSettings)?.ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    missingParameters.Append(key + " ");
                }
            }

            if (!string.IsNullOrWhiteSpace(missingParameters.ToString()))
            {
                ExceptionHandling.ArgumentException(missingParameters.ToString());
                Environment.Exit(-1);
            }
        }

    }

    public class SettingsFile
    {
        public string SettingsFilePath { get; set; }
    }
}
