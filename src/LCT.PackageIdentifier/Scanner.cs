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
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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

            string[] foundConfigFiles;
            IFileOperations fileOperations = new FileOperations();
            List<string> allFoundConfigFiles = new List<string>();
            if (config?.Include == null && config?.Exclude == null)
            {
                Logger.Error("Inclusion/Exclusion list is not provided!!Unable to identify the files\nPlease check if you have given a valid settings file");
                throw new ArgumentNullException($"Inlude:{config?.Include} or Exclude:{config?.Exclude} in config is found to be empty");

            }
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                Logger.Error("No root path given.Provide a valid input file path");
                throw new ArgumentException($"Invalid value for the {nameof(rootPath)} - {rootPath}");
            }

            if (!System.IO.Directory.Exists(rootPath))
            {
                Logger.Error("Root path does not exist.Provide a valid  path");
                throw new DirectoryNotFoundException($"The {nameof(rootPath)}  is not found at this path" +
               $" - {rootPath}");
            }

            Logger.Logger.Log(null, Level.Notice, $"Directory Location: Packages are read from the below locations:", null);
            foreach (string includePattern in config.Include)
            {
                foundConfigFiles = System.IO.Directory.GetFiles(rootPath, includePattern, SearchOption.AllDirectories);

                if (foundConfigFiles != null && foundConfigFiles.Length > 0)
                {
                    foreach (string configFile in foundConfigFiles)
                    {
                        CheckingForExcludedFiles(config, fileOperations, allFoundConfigFiles, configFile);
                    }
                }
            }

            if (allFoundConfigFiles.Count == 0)
            {
                Logger.Error("Provided package file path do not contain valid input files.");
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
