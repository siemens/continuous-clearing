// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        /// Copies the contents of the source directory to the target directory.
        /// </summary>
        /// <param name="sourceDirectory">The path of the source directory.</param>
        /// <param name="targetDirectory">The path of the target directory.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        public bool CopyToTargetDirectory(string sourceDirectory, string targetDirectory);

        /// <summary>
        /// Zips files to the specified target directory.
        /// </summary>
        /// <param name="targetDirectory">The path of the target directory where files will be zipped.</param>
        /// <returns>True if the zip operation was successful; otherwise, false.</returns>
        public bool ZipFileToTargetDirectory(string targetDirectory);

        /// <summary>
        /// Validates the given folder path.
        /// </summary>
        /// <param name="folderPath">The path of the folder to validate.</param>
        /// <returns>void.</returns>
        public void ValidateFolderPath(string folderPath);
    }
}
