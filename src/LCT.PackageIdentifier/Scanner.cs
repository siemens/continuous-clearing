// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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

            if (!Directory.Exists(rootPath))
            {
                Logger.Error("Root path does not exist.Provide a valid  path");
                throw new DirectoryNotFoundException($"The {nameof(rootPath)}  is not found at this path" +
               $" - {rootPath}");
            }

            Logger.Logger.Log(null, Level.Notice, $"\n \n Directory Location: Packages are read from the below locations:  \n", null);
            foreach (string includePattern in config.Include)
            {
                foundConfigFiles = Directory.GetFiles(rootPath, includePattern, SearchOption.AllDirectories);

                if (foundConfigFiles != null && foundConfigFiles.Length > 0)
                {
                    foreach (string configFile in foundConfigFiles)
                    {
                        CheckingForExcludedFiles(config, fileOperations, allFoundConfigFiles, configFile);
                    }                  
                }             
            }
            Logger.Logger.Log(null, Level.Notice, $"\n----------------------------------------------------", null);
            return allFoundConfigFiles;

        }

        private static void CheckingForExcludedFiles(Config config, IFileOperations fileOperations, List<string> allFoundConfigFiles, string configFile)
        {
            if (!IsExcluded(configFile, config.Exclude))
            {
                string currentDirectory = Path.GetDirectoryName(configFile);
                Logger.Logger.Log(null, Level.Info, $"  {currentDirectory} \n", null);

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
