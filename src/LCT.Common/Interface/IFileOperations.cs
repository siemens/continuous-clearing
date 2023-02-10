// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        /// Writes the given content to the file
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="dataToWrite">Data  to write</param>
        /// <param name="folderPath">Folder path to save the file</param>
        /// <param name="fileNameWithExtension">File Name with Extension</param>
        public string WriteContentToFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName);

        /// <summary>
        /// Validatest the given file path
        /// </summary>
        /// <param name="filePath">filePath</param>
        public void ValidateFilePath(string filePath);

        /// <summary>
        /// combines the comparisonBom data
        /// </summary>
        /// <param name="components">comparisonBOM data</param>
        /// <param name="filePath">filePath</param>
        public Bom CombineComponentsFromExistingBOM(Bom components, string filePath);

        /// <summary>
        /// writes to existing cycloneDx bom
        /// </summary>
        /// <param name="dataToWrite">comparisonBOM data</param>
        /// <param name="filePath">filePath</param>
        public string WriteContentToCycloneDXFile<T>(T dataToWrite, string filePath, string fileNameWithExtension);
    }
}
