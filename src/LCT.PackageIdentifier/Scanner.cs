// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Interface;
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

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        public static List<string> FileScanner(string rootPath, Config config)
        {

            string[] foundConfigFiles;
            IFileOperations fileOperations = new FileOperations();
            List<string> allFoundConfigFiles = new List<string>();
            if (config?.Include == null && config?.Exclude == null)
            {
                LogHandlingHelper.BasicErrorHandling("File Scanning", "FileScanner()", "Inclusion/Exclusion list is not provided. Unable to identify the files.", "Please check if you have provided a valid settings file with inclusion/exclusion patterns.");
                Logger.Error("Inclusion/Exclusion list is not provided!!Unable to identify the files\nPlease check if you have given a valid settings file");
                environmentHelper.CallEnvironmentExit(-1);

            }
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                LogHandlingHelper.BasicErrorHandling("File Scanning", "FileScanner()", "No root path provided.", "Provide a valid input file path.");
                Logger.Error("No root path given.Provide a valid input file path");
                environmentHelper.CallEnvironmentExit(-1);
            }

            if (!System.IO.Directory.Exists(rootPath))
            {
                LogHandlingHelper.BasicErrorHandling("File Scanning", "FileScanner()", "Root path does not exist.", "Provide a valid path.");
                Logger.Error("Root path does not exist.Provide a valid  path");
                environmentHelper.CallEnvironmentExit(-1);
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
                LogHandlingHelper.BasicErrorHandling("File scanning failed due to no valid input files found.", "FileScanner()", $"The provided package file path does not contain any valid input files. Please check the input path and inclusion/exclusion patterns.", "provide valid input files");
                Logger.Error("Provided package file path do not contain valid input files.");
                environmentHelper.CallEnvironmentExit(-1);
            }


            return allFoundConfigFiles;

        }

        private static void CheckingForExcludedFiles(Config config, IFileOperations fileOperations, List<string> allFoundConfigFiles, string configFile)
        {
            if (!IsExcluded(configFile, config.Exclude))
            {
                Logger.Logger.Log(null, Level.Info, $"    Input file FOUND :{configFile}", null);

                allFoundConfigFiles.Add(configFile);
                fileOperations.ValidateFilePath(configFile);
            }
            else
            {
                Logger.Debug($"\tSkipping '{configFile}' due to exclusion pattern.");
            }
        }

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
    }
}
