// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Constants;
using LCT.Common.Interface;
using log4net;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security;

namespace LCT.Common
{
    /// <summary>
    /// The FolderAction class
    /// </summary>
    public class FolderAction : IFolderAction
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Copies source directory content to target directory content
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="targetDirectory"></param>
        /// <returns>bool</returns>
        public bool CopyToTargetDirectory(string sourceDirectory, string targetDirectory)
        {
            Logger.Debug("CopyToTargetDirectory(): Start copying directory.");
            bool isCopied;
            try
            {
                var diSource = new DirectoryInfo(sourceDirectory);
                var diTarget = new DirectoryInfo(targetDirectory);

                CopyAll(diSource, diTarget);
                isCopied = true;
                Logger.Debug($"CopyToTargetDirectory(): Successfully copied from '{sourceDirectory}' to '{targetDirectory}'.");
            }
            catch (IOException ex)
            {
                isCopied = false;
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Directory Copy Error", $"Failed to copy directory from '{sourceDirectory}' to '{targetDirectory}'.", ex, "An I/O error occurred during the copy operation. Ensure the source and target directories are accessible.");
                Logger.Error("FolderAction.CopyToTargetDirectory():", ex);
            }
            catch (SecurityException ex)
            {
                isCopied = false;
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Directory Copy Error", $"Failed to copy directory from '{sourceDirectory}' to '{targetDirectory}'.", ex, "A security exception occurred. Ensure the application has the required permissions to access the directories.");
                Logger.Error("FolderAction.CopyToTargetDirectory():", ex);
            }

            Logger.Debug("FolderAction.CopyToTargetDirectory(): End copying directory.");

            return isCopied;
        }

        /// <summary>
        /// Validates the folder path given
        /// </summary>
        /// <param name="folderPath"></param>
        public void ValidateFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                LogHandlingHelper.ExceptionErrorHandling("Validation Error", "Validation failed for folder path.", new ArgumentException($"Invalid value for folderPath - {folderPath}"), "The provided folder path is null, empty, or consists only of whitespace.");
                throw new ArgumentException($"Invalid value for folderPath -{folderPath}");
            }

            if (!System.IO.Directory.Exists(folderPath))
            {
                LogHandlingHelper.ExceptionErrorHandling("Validation Error", "Folder path does not exist.", new DirectoryNotFoundException($"Invalid folder path - {folderPath}"), $"Ensure the folder exists at the specified path: {folderPath}");
                throw new DirectoryNotFoundException($"Invalid folder path -{folderPath}");
            }
        }

        /// <summary>
        /// Zip Files To Target Directory
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <returns>bool</returns>
        public bool ZipFileToTargetDirectory(string targetDirectory)
        {
            Logger.Debug("FolderAction.ZipFileToTargetDirectory(): Start zipping directory.");

            bool isZiped;
            try
            {
                string startPath = targetDirectory;
                string zipPath = targetDirectory + FileConstant.ZipFileExtension;

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                ZipFile.CreateFromDirectory(startPath, zipPath);
                isZiped = true;
                Logger.Debug($"FolderAction.ZipFileToTargetDirectory(): Successfully zipped directory '{startPath}' to '{zipPath}'.");
            }
            catch (IOException ex)
            {
                isZiped = false;
                LogHandlingHelper.ExceptionErrorHandling("Zipping Error", $"Failed to zip directory '{targetDirectory}'.", ex, "An I/O error occurred during the zipping process. Ensure the directory is accessible and not in use.");
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                isZiped = false;
                LogHandlingHelper.ExceptionErrorHandling("Zipping Error", $"Failed to zip directory '{targetDirectory}'.", ex, "Unauthorized access occurred. Ensure the application has the required permissions to access the directory.");
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }
            catch (NotSupportedException ex)
            {
                isZiped = false;
                LogHandlingHelper.ExceptionErrorHandling("Zipping Error", $"Failed to zip directory '{targetDirectory}'.", ex, "The operation is not supported. Ensure the directory path is valid and supported for zipping.");
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }

            Logger.Debug("FolderAction.ZipFileToTargetDirectory(): End zipping directory.");
            return isZiped;
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Logger.Debug($"FolderAction.CopyAll(): Start copying from '{source.FullName}' to '{target.FullName}'.");

            try
            {
                System.IO.Directory.CreateDirectory(target.FullName);

                // Copy each file into the new directory.
                foreach (FileInfo fi in source.GetFiles())
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }

                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Copy Error", $"Failed to copy from '{source.FullName}' to '{target.FullName}'.", ex, "An I/O error occurred during the copy operation. Ensure the source and target directories are accessible.");
            }
            catch (SecurityException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Copy Error", $"Failed to copy from '{source.FullName}' to '{target.FullName}'.", ex, "A security exception occurred. Ensure the application has the required permissions to access the directories.");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Copy Error", $"Failed to copy from '{source.FullName}' to '{target.FullName}'.", ex, "Unauthorized access occurred. Ensure the application has the required permissions to access the directories.");
            }
            Logger.Debug($"FolderAction.CopyAll(): Finished copying from '{source.FullName}' to '{target.FullName}'.");
        }
    }
}
