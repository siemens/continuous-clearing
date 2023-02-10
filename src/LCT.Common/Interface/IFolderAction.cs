// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Interface
{
    /// <summary>
    /// IFolderAction Interface
    /// </summary>
    public interface IFolderAction
    {
        /// <summary>
        /// Copies source directory content to target directory content
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="targetDirectory"></param>
        /// <returns>bool</returns>
        public bool CopyToTargetDirectory(string sourceDirectory, string targetDirectory);

        /// <summary>
        /// Zip Files To Target Directory
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <returns>bool</returns>
        public bool ZipFileToTargetDirectory(string targetDirectory);

        /// <summary>
        /// Validates the given folder path
        /// </summary>
        /// <param name="filePath"></param>
        public void ValidateFolderPath(string folderPath);
    }
}
