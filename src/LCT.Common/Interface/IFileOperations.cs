// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;

namespace LCT.Common.Interface
{
    /// <summary>
    /// IfileOperations interface - responsible for writing , reading the file
    /// </summary>
    public interface IFileOperations
    {
        /// <summary>
        /// Writes the given content to a file in the specified folder.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write.</param>
        /// <param name="folderPath">The folder path to save the file.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name associated with the file.</param>
        /// <returns>The path to the written file.</returns>
        public string WriteContentToFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName);

        /// <summary>
        /// Writes the given content to an output BOM file in the specified folder.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write.</param>
        /// <param name="folderPath">The folder path to save the file.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name associated with the file.</param>
        /// <returns>The path to the written BOM file.</returns>
        public string WriteContentToOutputBomFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName);

        /// <summary>
        /// Validates the given file path.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <returns>void.</returns>
        public void ValidateFilePath(string filePath);

        /// <summary>
        /// Combines components from an existing BOM file.
        /// </summary>
        /// <param name="components">The BOM components to combine.</param>
        /// <param name="filePath">The file path of the existing BOM.</param>
        /// <returns>The combined BOM object.</returns>
        public Bom CombineComponentsFromExistingBOM(Bom components, string filePath);

        /// <summary>
        /// Writes the given content to an existing CycloneDX BOM file.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The BOM data to write.</param>
        /// <param name="filePath">The file path of the CycloneDX BOM.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <returns>The path to the written CycloneDX BOM file.</returns>
        public string WriteContentToCycloneDXFile<T>(T dataToWrite, string filePath, string fileNameWithExtension);

        /// <summary>
        /// Writes the given content to a report file for not approved items.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write.</param>
        /// <param name="folderPath">The folder path to save the file.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="name">The name associated with the report.</param>
        /// <returns>The path to the written report file.</returns>
        public string WriteContentToReportNotApprovedFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string name);

        /// <summary>
        /// Writes the given content to a file for multiple versions in the specified folder.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write.</param>
        /// <param name="folderPath">The folder path to save the file.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name associated with the file.</param>
        /// <returns>The path to the written file for multiple versions.</returns>
        public string WriteContentToMultipleVersionsFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName);
    }
}
