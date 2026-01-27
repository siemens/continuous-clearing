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
        #region Fields
        private const string FileScanningContext = "File Scanning";
        private const string FileScannerMethod = "FileScanner()";       
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static List<string> FileScanner(string rootPath, Config config, IEnvironmentHelper environmentHelper)
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Scans the specified rootPath for files matching include patterns from the config and returns the matching file paths,
        /// excluding those that match any exclusion patterns.
        /// </summary>
        /// <param name="rootPath">Root directory to scan for package files.</param>
        /// <param name="config">Configuration containing Include and Exclude patterns.</param>
        /// <returns>List of discovered file paths that match include patterns and are not excluded.</returns>
        public static List<string> FileScanner(string rootPath, Config config)
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

        private static void HandleNoValidFilesFound(IEnvironmentHelper environmentHelper)
        {
            LogHandlingHelper.BasicErrorHandling("File scanning failed due to no valid input files found.", FileScannerMethod, $"The provided package file path does not contain any valid input files. Please check the input path and inclusion/exclusion patterns.", "Provide valid input files");
            Logger.Error("Provided package file path does not contain valid input files.");
            environmentHelper.CallEnvironmentExit(-1);
        }


            return allFoundConfigFiles;

        }

        /// <summary>
        /// Validates a discovered file against the exclusion patterns and, when not excluded, logs and validates the file path.
        /// </summary>
        /// <param name="config">Config instance containing Exclude patterns.</param>
        /// <param name="fileOperations">File operations helper used to validate the file path.</param>
        /// <param name="allFoundConfigFiles">Accumulator list of discovered files to add to.</param>
        /// <param name="configFile">Path of the discovered file to check.</param>
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
                Logger.Debug($"\tSkipping '{configFile}' due to exclusion pattern.");
            }
        }

        /// <summary>
        /// Determines whether the provided file path matches any of the exclusion regular expressions.
        /// </summary>
        /// <param name="filePath">File path to evaluate.</param>
        /// <param name="exclusionPatterns">Array of regex patterns to test against. When null or empty, no exclusion is applied.</param>
        /// <returns>True if the file path matches any exclusion pattern; otherwise false.</returns>
        internal static bool IsExcluded(string filePath, string[] exclusionPatterns)
        {
            if (exclusionPatterns == null || exclusionPatterns.Length == 0)
                return false;

            foreach (string exclusionPattern in exclusionPatterns)
            {
                Regex exRegex = new Regex(exclusionPattern);
                if (exRegex.IsMatch(filePath))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Events
        #endregion
    }
}
