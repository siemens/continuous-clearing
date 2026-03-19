// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Scans the Directory
    /// </summary>
    public static class FolderScanner
    {

        private const string FileScanningContext = "File Scanning";
        private const string FileScannerMethod = "FileScanner()";
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Scans the specified root directory and its subdirectories for configuration files matching the include
        /// patterns defined in the provided configuration.
        /// </summary>       
        /// <param name="rootPath">The root directory path to begin scanning for configuration files. Cannot be null or empty.</param>
        /// <param name="config">The configuration object that specifies include patterns and other scanning options. Cannot be null.</param>
        /// <param name="environmentHelper">An environment helper used for environment-specific operations during scanning. Cannot be null.</param>
        /// <returns>A list of file paths to configuration files that match the include patterns. The list is empty if no
        /// matching files are found.</returns>
        public static List<string> FileScanner(string rootPath, Config config, IEnvironmentHelper environmentHelper)
        {
            ValidateInputs(rootPath, config, environmentHelper);

            Logger.Logger.Log(null, Level.Notice, $"Directory Location: Packages are read from the below locations:", null);

            List<string> allFoundConfigFiles = new List<string>();
            IFileOperations fileOperations = new FileOperations();

            foreach (string includePattern in config.Include)
            {
                ProcessIncludePattern(rootPath, includePattern, config, fileOperations, allFoundConfigFiles);
            }

            if (allFoundConfigFiles.Count == 0)
            {
                HandleNoValidFilesFound(environmentHelper);
            }

            return allFoundConfigFiles;
        }

        /// <summary>
        /// Validates the input parameters required for file scanning and terminates the process if any validation
        /// fails.
        /// </summary>
        /// <param name="rootPath">The root directory path to scan. Must not be null, empty, or whitespace, and must refer to an existing
        /// directory.</param>
        /// <param name="config">The configuration object containing inclusion and exclusion patterns. At least one of the Include or Exclude
        /// lists must be provided.</param>
        /// <param name="environmentHelper">An environment helper used to terminate the process if validation fails.</param>
        private static void ValidateInputs(string rootPath, Config config, IEnvironmentHelper environmentHelper)
        {
            if (config?.Include == null && config?.Exclude == null)
            {
                LogHandlingHelper.BasicErrorHandling(FileScanningContext, FileScannerMethod, "Inclusion/Exclusion list is not provided. Unable to identify the files.", "Please check if you have provided a valid settings file with inclusion/exclusion patterns.");
                Logger.ErrorFormat("Inlude:{0} or Exclude:{1} in config is found to be empty,Inclusion/Exclusion list is not provided!!Unable to identify the files\nPlease check if you have given a valid settings file", config?.Include, config?.Exclude);
                environmentHelper.CallEnvironmentExit(-1);
            }

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                LogHandlingHelper.BasicErrorHandling(FileScanningContext, FileScannerMethod, string.Format("No root path provided at{0} - {1}.", nameof(rootPath), rootPath), "Provide a valid input file path.");
                Logger.ErrorFormat("Invalid value for the {0} - {1},No root path given.Provide a valid input file path", nameof(rootPath), rootPath);
                environmentHelper.CallEnvironmentExit(-1);
            }

            if (!System.IO.Directory.Exists(rootPath))
            {
                LogHandlingHelper.BasicErrorHandling(FileScanningContext, FileScannerMethod, string.Format("Root path does not exist at {0}.", rootPath), "Provide a valid path.");
                Logger.ErrorFormat("The {0}  is not found at this path - {1},Root path does not exist.Provide a valid  path", nameof(rootPath), rootPath);
                environmentHelper.CallEnvironmentExit(-1);
            }
        }

        /// <summary>
        /// Searches for configuration files in the specified root directory that match the given include pattern and
        /// processes each found file, excluding any files as determined by the configuration.
        /// </summary>
        /// <param name="rootPath">The root directory in which to search for configuration files.</param>
        /// <param name="includePattern">The search pattern to use when locating configuration files. This pattern supports standard file system
        /// wildcards.</param>
        /// <param name="config">The configuration settings used to determine which files should be excluded from processing.</param>
        /// <param name="fileOperations">An object that provides file system operations used during file processing.</param>
        /// <param name="allFoundConfigFiles">A list that is populated with the full paths of all configuration files found and not excluded.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="includePattern"/> is null.</exception>
        private static void ProcessIncludePattern(string rootPath, string includePattern, Config config, IFileOperations fileOperations, List<string> allFoundConfigFiles)
        {
            try
            {
                string[] foundConfigFiles = System.IO.Directory.GetFiles(rootPath, includePattern, SearchOption.AllDirectories);

                if (foundConfigFiles != null && foundConfigFiles.Length > 0)
                {
                    foreach (string configFile in foundConfigFiles)
                    {
                        CheckingForExcludedFiles(config, fileOperations, allFoundConfigFiles, configFile);
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                Logger.ErrorFormat("Error occurred while scanning files with pattern '{0}' in path '{1}': {2}", includePattern, rootPath, ex.Message);
                LogHandlingHelper.BasicErrorHandling(FileScanningContext, FileScannerMethod, string.Format("Error occurred while scanning files with pattern '{0}' in path '{1}'.", includePattern, rootPath), "Check the input path and inclusion patterns.");
                throw new ArgumentNullException(nameof(includePattern), $"Error occurred while scanning files with pattern '{includePattern}' in path '{rootPath}'.\nInnerException: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the scenario where no valid input files are found during file scanning and terminates the process
        /// with an error code.
        /// </summary>
        /// <remarks>This method logs an error message and calls the environment exit routine with a
        /// non-zero exit code to indicate failure. It should be called when file scanning cannot proceed due to missing
        /// or invalid input files.</remarks>
        /// <param name="environmentHelper">An implementation of the environment helper used to exit the process with an error code.</param>
        private static void HandleNoValidFilesFound(IEnvironmentHelper environmentHelper)
        {
            LogHandlingHelper.BasicErrorHandling("File scanning failed due to no valid input files found.", FileScannerMethod, $"The provided package file path does not contain any valid input files. Please check the input path and inclusion/exclusion patterns.", "Provide valid input files");
            Logger.Error("Provided package file path does not contain valid input files.");
            environmentHelper.CallEnvironmentExit(-1);
        }

        /// <summary>
        /// Checks whether the specified configuration file is excluded based on the provided exclusion patterns and, if
        /// not excluded, adds it to the list of found configuration files and validates its path.
        /// </summary>       
        /// <param name="config">The configuration settings containing exclusion patterns used to determine whether the file should be
        /// processed.</param>
        /// <param name="fileOperations">An object that provides file-related operations, used to validate the configuration file path.</param>
        /// <param name="allFoundConfigFiles">A list that collects the paths of configuration files that are not excluded and have been found during
        /// processing.</param>
        /// <param name="configFile">The path to the configuration file to check against the exclusion patterns.</param>
        private static void CheckingForExcludedFiles(Config config, IFileOperations fileOperations, List<string> allFoundConfigFiles, string configFile)
        {
            if (!IsExcluded(configFile, config.Exclude))
            {
                LoggerHelper.ValidFilesInfoDisplayForCli(configFile);
                allFoundConfigFiles.Add(configFile);
                fileOperations.ValidateFilePath(configFile);
            }
            else
            {
                Logger.DebugFormat("\tSkipping '{0}' due to exclusion pattern.", configFile);
            }
        }

        /// <summary>
        /// Determines whether the specified file path matches any of the provided exclusion patterns.
        /// </summary>       
        /// <param name="filePath">The full path of the file to evaluate against the exclusion patterns.</param>
        /// <param name="exclusionPatterns">An array of regular expression patterns used to identify files to exclude. If null or empty, no files are
        /// excluded.</param>
        /// <returns>true if the file path matches at least one exclusion pattern; otherwise, false.</returns>
        internal static bool IsExcluded(string filePath, string[] exclusionPatterns)
        {
            if (exclusionPatterns == null || exclusionPatterns.Length == 0)
                return false;

            foreach (string exclusionPattern in exclusionPatterns)
            {
                Regex exRegex = new Regex(exclusionPattern, RegexOptions.None, TimeSpan.FromSeconds(5));
                if (exRegex.IsMatch(filePath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
