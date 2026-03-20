// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace LCT.Common
{
    /// <summary>
    /// The SettingsManager class
    /// </summary>
    public class SettingsManager : ISettingsManager
    {
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();

        #endregion

        #region Properties

        public string BasePath { get; private set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #endregion

        #region Methods

        /// <summary>
        /// Reads the Configuration from input args and json setting file
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>AppSettings</returns>
        public T ReadConfiguration<T>(string[] args, string jsonSettingsFileName, IEnvironmentHelper environmentHelper) where T : class
        {
            Logger.Debug("ReadConfiguration():Start reading configuration.");

            if (args != null)
            {
                string[] maskedArgs = CommonHelper.MaskSensitiveArguments(args);
                Logger.DebugFormat("ReadConfiguration():Commandline arguments: {0}", string.Join(" ", maskedArgs));
            }
            if (args?.Length == 0)
            {
                Logger.Debug("ReadConfiguration():No arguments provided through command line.");
                DisplayHelp();
                environmentHelper.CallEnvironmentExit(0);
            }
            string settingsFilePath = GetConfigFilePathFromArgs(args, jsonSettingsFileName);
            Logger.DebugFormat("Settings File: {0}", settingsFilePath);
            //add ut for reading - add json and then cmd args
            IConfigurationBuilder settingsConfigBuilder = new ConfigurationBuilder()
                                                                    .SetBasePath(BasePath)
                                                                    .AddJsonFile(settingsFilePath, true, true)
                                                                    .AddEnvironmentVariables()
                                                                    .AddCommandLine(args);


            IConfiguration settingsConfig;
            try
            {
                settingsConfig = settingsConfigBuilder.Build();
            }
            catch (InvalidDataException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("ReadConfiguration()", $"Failed to load configuration file. Please verify the JSON format in: {settingsFilePath}", ex, "InvalidDataException occurred while loading configuration.");
                Logger.ErrorFormat("Failed to load configuration file. Please verify the JSON format in: {0}", settingsFilePath);
                return default;
            }
            catch (FormatException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("ReadConfiguration()", $"Configuration file contains invalid format. Please check for missing quotes or invalid syntax in: {settingsFilePath}", ex, "FormatException occurred while loading configuration.");
                Logger.ErrorFormat("Configuration file contains invalid format. Please check for missing quotes or invalid syntax in: {0}", settingsFilePath);
                return default;
            }


            T appSettings = settingsConfig.Get<T>();

            if (appSettings == null)
            {
                LogHandlingHelper.ExceptionErrorHandling("ReadConfiguration()", $"Failed to load application settings. The configuration object is null.", new InvalidDataException(nameof(appSettings)), $"The application settings could not be loaded. Ensure the configuration file is valid and contains the required settings.");
                return default;
            }

            Logger.Debug("ReadConfiguration():Successfully completed configuration reading.");

            return appSettings;
        }

        /// <summary>
        /// Displays the CLI usage help from the help text file.
        /// </summary>
        public static void DisplayHelp()
        {

            StreamReader sr = new("CLIUsageNpkg.txt");
            //Read the whole file
            string line = sr.ReadToEnd();
            Console.WriteLine(line);

            sr.Dispose();
        }

        /// <summary>
        /// Gets the configuration file path from command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="jsonSettingsFileName">The default JSON settings file name.</param>
        /// <returns>The configuration file path.</returns>
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
                Logger.DebugFormat("SettingsFilePath: \"{0}\" does not exist. Using default settings.", settingsFilePath);
                return defaultFile;
            }

            return settingsFilePath;
        }

        /// <summary>
        /// Checks required arguments to run based on the current executable type.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="currentExe">The current executable type (Identifier, Creator, or other).</param>
        public void CheckRequiredArgsToRun(CommonAppSettings appSettings, string currentExe)
        {

            if (currentExe == "Identifier")
            {
                Logger.Debug("CheckRequiredArgsToRun():Validating mandatory parameters has started");
                //Required parameters to run Package Identifier
                List<string> identifierReqParameters = new List<string>()
                {
                    "Directory.InputFolder",
                    "Directory.OutputFolder",
                    "ProjectType"
                };

                if (appSettings.SW360 != null)
                {
                    identifierReqParameters.Add($"SW360.ProjectID");
                    identifierReqParameters.Add($"SW360.Token");
                    identifierReqParameters.Add($"SW360.URL");
                }
                if (appSettings.Jfrog != null)
                {
                    identifierReqParameters.Add($"Jfrog.Token");
                    identifierReqParameters.Add($"Jfrog.URL");
                }
                AddSbomSigningRequiredParameters(appSettings, identifierReqParameters);
                //Check if ProjectType contains a value and add InternalRepos key accordingly
                if (!string.IsNullOrWhiteSpace(appSettings.ProjectType) &&
                    appSettings.Jfrog != null &&
                    !appSettings.ProjectType.Equals("ALPINE", StringComparison.InvariantCultureIgnoreCase))
                {
                    identifierReqParameters.Add($"{appSettings.ProjectType}.Artifactory.InternalRepos");
                }
                CheckForMissingParameter(appSettings, identifierReqParameters);
            }
            else if (currentExe == "Creator")
            {
                //Required parameters to run SW360Component Creator
                List<string> creatorReqParameters = new List<string>()
            {
                "SW360.ProjectID",
                "Sw360.Token",
                "SW360.URL",
                "Directory.OutputFolder"
            };
                AddSbomSigningRequiredParameters(appSettings, creatorReqParameters);
                CheckForMissingParameter(appSettings, creatorReqParameters);
            }
            else
            {
                //Required parameters to run Artifactory Uploader
                List<string> uploaderReqParameters = new List<string>()
            {
                "Jfrog.URL",
                "Directory.OutputFolder",
                "Jfrog.Token",
            };
                AddSbomSigningRequiredParameters(appSettings, uploaderReqParameters);

                CheckForMissingParameter(appSettings, uploaderReqParameters);
            }
            Logger.Debug("CheckRequiredArgsToRun():Validating mandatory parameters has completed\n");
        }
        // <summary>
        /// Adds SBOM signing required parameters to the parameter list if SBOM signing is enabled.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="requiredParameters">The list of required parameters to add SBOM signing parameters to.</param>
        private static void AddSbomSigningRequiredParameters(CommonAppSettings appSettings, List<string> requiredParameters)
        {
            if (appSettings.SbomSigning.SBOMSignVerify)
            {
                requiredParameters.Add("SbomSigning.KeyVaultURI");
                requiredParameters.Add("SbomSigning.CertificateName");
                requiredParameters.Add("SbomSigning.ClientId");
                requiredParameters.Add("SbomSigning.ClientSecret");
                requiredParameters.Add("SbomSigning.TenantId");
            }
        }
        /// <summary>
        /// Checks for missing required parameters in the application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="reqParameters">The list of required parameter keys.</param>
        private static void CheckForMissingParameter(CommonAppSettings appSettings, List<string> reqParameters)
        {
            StringBuilder missingParameters = new StringBuilder();
            Logger.DebugFormat("CheckForMissingParameter(): Required Parameters: {0}", string.Join(", ", reqParameters));
            foreach (string key in reqParameters)
            {
                object currentObject = GetNestedPropertyValue(appSettings, key);

                if (IsMissingValue(currentObject))
                {
                    missingParameters.Append(key + "\n");
                }
            }

            if (missingParameters.Length > 0)
            {
                Logger.DebugFormat("HandleMissingParameters(): Missing Parameters: {0}", missingParameters.ToString().Trim());
                ExceptionHandling.ArgumentException(missingParameters.ToString());
                environmentHelper.CallEnvironmentExit(-1);
            }
        }

        /// <summary>
        /// Gets the nested property value from an object using dot notation.
        /// </summary>
        /// <param name="obj">The object to retrieve the property value from.</param>
        /// <param name="key">The property key in dot notation (e.g., "Property.SubProperty").</param>
        /// <returns>The property value, or null if not found.</returns>
        private static object GetNestedPropertyValue(object obj, string key)
        {
            string[] parts = key.Split('.');
            object currentObject = obj;

            foreach (string part in parts)
            {
                if (currentObject == null)
                {
                    break;
                }

                PropertyInfo property = currentObject.GetType().GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                currentObject = property?.GetValue(currentObject);
            }

            return currentObject;
        }

        /// <summary>
        /// Checks if a value is missing, null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is missing; otherwise, false.</returns>
        private static bool IsMissingValue(object value)
        {
            if (value is Array array)
            {
                return array.Length == 0 || string.IsNullOrWhiteSpace(array.GetValue(0)?.ToString());
            }

            if (value is IList<object> list)
            {
                return list.Count == 0 || string.IsNullOrWhiteSpace(list[0]?.ToString());
            }

            return string.IsNullOrWhiteSpace(value?.ToString());
        }

        /// <summary>
        /// Checks if Azure DevOps debug mode is enabled.
        /// </summary>
        /// <returns>True if Azure DevOps debug mode is enabled; otherwise, false.</returns>
        public static bool IsAzureDevOpsDebugEnabled()
        {
            string azureDevOpsDebug = Environment.GetEnvironmentVariable("System.Debug") ?? string.Empty;
            if (bool.TryParse(azureDevOpsDebug, out bool systemDebugEnabled) && systemDebugEnabled)
            {
                return true;
            }
            return false;
        }

        #endregion
    }

    /// <summary>
    /// Represents settings file configuration.
    /// </summary>
    public class SettingsFile
    {
        #region Properties

        public string SettingsFilePath { get; set; }

        #endregion
    }
}
