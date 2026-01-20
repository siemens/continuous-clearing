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
        #region Fields

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Methods

        /// <summary>
        /// Copies source directory content to target directory content
        /// </summary>
        /// <param name="sourceDirectory">The source directory path.</param>
        /// <param name="targetDirectory">The target directory path.</param>
        /// <returns>True if the copy operation succeeded; otherwise, false.</returns>
        public bool CopyToTargetDirectory(string sourceDirectory, string targetDirectory)
        {
            Logger.Debug("FolderAction.CopyToTargetDirectory():Start");
            bool isCopied;
            try
            {
                var diSource = new DirectoryInfo(sourceDirectory);
                var diTarget = new DirectoryInfo(targetDirectory);

                CopyAll(diSource, diTarget);
                isCopied = true;
            }
            catch (IOException ex)
            {
                isCopied = false;
                Environment.ExitCode = -1;
                Logger.Error("FolderAction.CopyToTargetDirectory():", ex);
            }
            catch (SecurityException ex)
            {
                isCopied = false;
                Environment.ExitCode = -1;
                Logger.Error("FolderAction.CopyToTargetDirectory():", ex);
            }

            Logger.Debug("FolderAction.CopyToTargetDirectory():End");

            return isCopied;
        }

        /// <summary>
        /// Validates the folder path given
        /// </summary>
        /// <param name="folderPath">The folder path to validate.</param>
        public void ValidateFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException($"Invalid value for folderPath -{folderPath}");
            }

            if (!System.IO.Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Invalid folder path -{folderPath}");
            }
        }

        /// <summary>
        /// Zip Files To Target Directory
        /// </summary>
        /// <param name="targetDirectory">The target directory to create a zip file from.</param>
        /// <returns>True if the zip operation succeeded; otherwise, false.</returns>
        public bool ZipFileToTargetDirectory(string targetDirectory)
        {
            Logger.Debug("FolderAction.ZipFileToTargetDirectory():Start");

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
            }
            catch (IOException ex)
            {
                isZiped = false;
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                isZiped = false;
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }
            catch (NotSupportedException ex)
            {
                isZiped = false;
                Logger.Error("FolderAction.ZipFileToTargetDirectory():", ex);
            }

            Logger.Debug("FolderAction.ZipFileToTargetDirectory():End");
            return isZiped;
        }

        /// <summary>
        /// Recursively copies all files and subdirectories from source to target directory.
        /// </summary>
        /// <param name="source">The source directory information.</param>
        /// <param name="target">The target directory information.</param>
        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Logger.Debug("FolderAction.CopyAll():Start");

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
                Logger.Error("FolderAction.CopyAll():", ex);
            }
            catch (SecurityException ex)
            {
                Logger.Error("FolderAction.CopyAll():", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error("FolderAction.CopyAll():", ex);
            }
            Logger.Debug("FolderAction.CopyAll():End");
        }

        #endregion
    }
}
