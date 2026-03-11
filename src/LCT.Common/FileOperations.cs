// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;

namespace LCT.Common
{
    /// <summary>
    /// FileOperations class - responsible for writing , reading the file
    /// </summary>
    public class FileOperations : IFileOperations
    {
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        static readonly SbomSigningValidation sbomSigningValidation = new();

        #endregion

        #region Properties

        public static string CatoolBomFilePath { get; set; }
        private const string FileOperationsMessage = "File Operations";
        private const string LogMessage = "Generated FilePath: {0}";

        #endregion

        #region Methods

        /// <summary>
        /// Validates that the file path exists and is not null or whitespace.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        public void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHandlingHelper.ExceptionErrorHandling($"Given file path is empty or null", "Method:ValidateFilePath()", new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}"), "Provide valid file path");
                throw new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}");
            }

            if (!File.Exists(filePath))
            {
                LogHandlingHelper.ExceptionErrorHandling($"File not exist in given path", "Method:ValidateFilePath()", new FileNotFoundException($"Invalid value for the {nameof(filePath)} - {filePath}"), "Provide valid file path");
                throw new FileNotFoundException($"The {nameof(filePath)}  is not found at this path- {filePath}");
            }
        }

        /// <summary>
        /// writes the content to the specified file
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write to the file.</param>
        /// <param name="folderPath">The folder path where the file will be written.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name to prefix the file name.</param>
        /// <returns>"success" if the operation succeeded; otherwise, "failure".</returns>
        public string WriteContentToFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            try
            {
                Logger.DebugFormat("WriteContentToFile(): Starting to write content to file. FolderPath: {0}, FileName: {1}, ProjectName: {2}", folderPath, fileNameWithExtension, projectName);

                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);

                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.DebugFormat(LogMessage, filePath);

                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, jsonString);
                Logger.Debug("WriteContentToFile():Content successfully written to file.");
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToFile():Completed writing content to the file.\n");
            return "success";

        }

        /// <summary>
        /// Writes the content to the output BOM file.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write to the BOM file.</param>
        /// <param name="folderPath">The folder path where the file will be written.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name to prefix the file name.</param>
        /// <returns>"success" if the operation succeeded; otherwise, "failure".</returns>
        public string WriteContentToOutputBomFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName, CommonAppSettings appSettings)
        {
            try
            {
                Logger.DebugFormat("WriteContentToOutputBomFile(): Starting to write BOM content to file. FolderPath: {0}, FileName: {1}, ProjectName: {2}", folderPath, fileNameWithExtension, projectName);
                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = CatoolBomFilePath = Path.Combine(folderPath, fileName);
                Logger.DebugFormat(LogMessage, filePath);

                BackupTheGivenFile(folderPath, fileName);
                string bomContent = dataToWrite.ToString();
                if (appSettings.SbomSigning.SBOMSignVerify)
                {
                    try
                    {
                        bomContent = sbomSigningValidation.PerformSbomSigning(appSettings, "sign", filePath, bomContent);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string errorMsg = $"SBOM signing failed: {ex.Message}";
                        Logger.Error(errorMsg, ex);
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                    catch (ArgumentException ex)
                    {
                        string errorMsg = $"SBOM signing failed: Configuration error - {ex.Message}";
                        Logger.Error(errorMsg, ex);
                        environmentHelper.CallEnvironmentExit(-1);
                    }                    
                }
                File.WriteAllText(filePath, bomContent);
                Logger.Debug("WriteContentToOutputBomFile():Content successfully written to file.");               

            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToOutputBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToOutputBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling(FileOperationsMessage, "WriteContentToOutputBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToOutputBomFile():Completed writing content to the file.");
            return "success";

        }

        /// <summary>
        /// Combines components from an existing BOM file with new components.
        /// </summary>
        /// <param name="components">The new BOM components to combine.</param>
        /// <param name="filePath">The file path of the existing BOM file.</param>
        /// <returns>A BOM containing the combined components and dependencies.</returns>
        public Bom CombineComponentsFromExistingBOM(Bom components, string filePath)
        {
            Bom comparisonData = new Bom();

            try
            {

                if (File.Exists(filePath))
                {

                    StreamReader fileRead = new StreamReader(filePath);
                    var content = fileRead.ReadToEnd();
                    try
                    {
                        comparisonData = JsonConvert.DeserializeObject<Bom>(content);
                    }
                    catch (JsonSerializationException)
                    {
                        comparisonData = CycloneDX.Json.Serializer.Deserialize(content);
                    }

                    fileRead.Close();
                    List<Component> list = new(comparisonData.Components.Count + components.Components.Count);
                    list.AddRange(comparisonData.Components);
                    list.AddRange(components.Components);
                    comparisonData.Components = list;

                    comparisonData.Components = comparisonData.Components?.GroupBy(x => new { x.Name, x.Version }).Select(y => y.First()).ToList();

                    if (comparisonData.Dependencies != null && comparisonData.Dependencies.Count > 0)
                    {
                        comparisonData.Dependencies.AddRange(components.Dependencies);
                    }
                    else
                    {
                        comparisonData.Dependencies = components.Dependencies;
                    }
                    comparisonData.Dependencies = comparisonData.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
                    //Update Compositions section
                    UpdateCompositions(ref components, ref comparisonData);
                }
                else
                {
                    LogHandlingHelper.BasicErrorHandling($"Combine components in already existing Bomfile", "CombineComponentsFromExistingBOM()", $"Invalid path entered. Please check in {filePath} if the comparison BOM path entered is correct.", "Ensure the file path is correct and the file exists.");
                    Logger.Error($"Error:Invalid path entered,Please check if the comparison BOM  path entered is correct");
                    throw new FileNotFoundException();
                }
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Combine components in already existing Bomfile", "CombineComponentsFromExistingBOM()", e);
                Environment.ExitCode = -1;
                Logger.Error($"Error:Invalid path entered,Please check if the comparison BOM  path entered is correct", e);
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Combine components in already existing Bomfile", "CombineComponentsFromExistingBOM()", e);
                Environment.ExitCode = -1;
                Logger.Error($"Error:Invalid path entered,Please check if the comparison BOM path entered is correct", e);
            }
            return comparisonData;
        }

        /// <summary>
        /// Writes the content to a CycloneDX file.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write to the file.</param>
        /// <param name="filePath">The folder path where the file will be written.</param>
        /// <param name="fileNameWithExtension">The file name with extension to copy and write.</param>
        /// <returns>"success" if the operation succeeded; otherwise, "failure".</returns>
        public string WriteContentToCycloneDXFile<T>(T dataToWrite, string filePath, string fileNameWithExtension)
        {
            try
            {
                Logger.DebugFormat("WriteContentToCycloneDXFile(): Starting to write content to CycloneDX file. FolderPath: {0}, FileName: {1}", filePath, fileNameWithExtension);
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);
                string filename = Path.GetFileName(fileNameWithExtension);
                filePath = $"{filePath}\\{filename}";
                File.Copy(fileNameWithExtension, filePath);
                File.WriteAllText(filePath, jsonString);
                Logger.Debug("WriteContentToCycloneDXFile():Content successfully written to CycloneDX file.");

            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to CycloneDX File", "WriteContentToCycloneDXFile()", e, $"FolderPath: {filePath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to CycloneDX File", "WriteContentToCycloneDXFile()", e, $"FolderPath: {filePath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to CycloneDX File", "WriteContentToCycloneDXFile()", e, $"FolderPath: {filePath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToCycloneDXFile():Completed writing content to the file.");
            return "success";

        }

        /// <summary>
        /// Backs up the given file by moving it with a timestamp prefix.
        /// </summary>
        /// <param name="folderPath">The folder path containing the file to backup.</param>
        /// <param name="fileName">The file name to backup.</param>
        private static void BackupTheGivenFile(string folderPath, string fileName)
        {
            string oldFile = Path.Combine(folderPath, fileName);
            string newFile = string.Format("{0}/{1:MM-dd-yyyy_HHmm_ss}_{2}_{3}", folderPath, DateTime.Now, FileConstant.backUpKey, fileName);
            Logger.DebugFormat("BackupTheGivenFile(): Starting backup process. OldFile: {0}, NewFile: {1}", oldFile, newFile);
            try
            {
                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile);
                    Logger.DebugFormat("BackupTheGivenFile(): Successfully created backup of {0} at {1}", oldFile, newFile);
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Logger.Error($"Error occurred while generating backup BOM file", ex);
                Environment.ExitCode = -1;
            }
            catch (NotSupportedException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Logger.Error($"Error occurred while generating backup BOM file", ex);
                Environment.ExitCode = -1;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Logger.Error($"Error occurred while generating backup BOM file", ex);
                Environment.ExitCode = -1;
            }
        }

        /// <summary>
        /// Writes the content to a report file for not approved items.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write to the file.</param>
        /// <param name="folderPath">The folder path where the file will be written.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="name">The name to prefix the file name.</param>
        /// <returns>"success" if the operation succeeded; otherwise, "failure".</returns>
        public string WriteContentToReportNotApprovedFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string name)
        {
            try
            {
                Logger.DebugFormat("WriteContentToReportNotApprovedFile(): Starting to write content to Report Not Approved file. FolderPath: {0}, FileName: {1}, Name: {2}", folderPath, fileNameWithExtension, name);
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{name}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.DebugFormat(LogMessage, filePath);
                File.WriteAllText(filePath, jsonString);
                Logger.Debug("WriteContentToReportNotApprovedFile():Content successfully written to the file.");
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Report Not Approved File", "WriteContentToReportNotApprovedFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Report Not Approved File", "WriteContentToReportNotApprovedFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Report Not Approved File", "WriteContentToReportNotApprovedFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToReportNotApprovedFile():Completed writing content to the file.");
            return "success";

        }

        /// <summary>
        /// Writes the content to a file for tracking multiple versions.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="dataToWrite">The data to write to the file.</param>
        /// <param name="folderPath">The folder path where the file will be written.</param>
        /// <param name="fileNameWithExtension">The file name with extension.</param>
        /// <param name="projectName">The project name to prefix the file name.</param>
        /// <returns>"success" if the operation succeeded; otherwise, "failure".</returns>
        public string WriteContentToMultipleVersionsFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            try
            {
                Logger.DebugFormat("WriteContentToMultipleVersionsFile(): Starting to write content to Multiple Versions file. FolderPath: {0}, FileName: {1}, ProjectName: {2}", folderPath, fileNameWithExtension, projectName);
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.DebugFormat(LogMessage, filePath);
                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, jsonString);
                Logger.Debug("WriteContentToMultipleVersionsFile():Content successfully written to the file.");
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Multiple Versions File", "WriteContentToMultipleVersionsFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Multiple Versions File", "WriteContentToMultipleVersionsFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Write content to Multiple Versions File", "WriteContentToMultipleVersionsFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToMultipleVersionsFile():Completed writing content to the file.");
            return "success";

        }

        /// <summary>
        /// Updates the compositions section by merging source and target BOM compositions.
        /// </summary>
        /// <param name="components">The source BOM with compositions to merge.</param>
        /// <param name="comparisonData">The target BOM to update with compositions.</param>
        private static void UpdateCompositions(ref Bom components, ref Bom comparisonData)
        {
            // Early return if there are no compositions to process
            if (components.Compositions == null || components.Compositions.Count == 0)
            {
                return;
            }

            // If target has no compositions, simply assign the source compositions
            if (comparisonData.Compositions == null || comparisonData.Compositions.Count == 0)
            {
                comparisonData.Compositions = components.Compositions;
                return;
            }

            // Process each source composition
            foreach (var sourceComposition in components.Compositions)
            {
                var matchingComposition = FindMatchingComposition(comparisonData.Compositions, sourceComposition);

                if (matchingComposition != null)
                {
                    MergeDependencies(sourceComposition, matchingComposition);
                }
                else
                {
                    comparisonData.Compositions.Add(sourceComposition);
                }
            }
        }

        /// <summary>
        /// Finds a matching composition in the compositions list based on assembly equality.
        /// </summary>
        /// <param name="compositions">The list of compositions to search.</param>
        /// <param name="sourceComposition">The source composition to match.</param>
        /// <returns>The matching composition if found; otherwise, null.</returns>
        private static Composition FindMatchingComposition(List<Composition> compositions, Composition sourceComposition)
        {
            return compositions.FirstOrDefault(c =>
                c.Assemblies != null &&
                sourceComposition.Assemblies != null &&
                c.Assemblies.SequenceEqual(sourceComposition.Assemblies));
        }

        /// <summary>
        /// Merges dependencies from source composition to target composition.
        /// </summary>
        /// <param name="source">The source composition with dependencies to merge.</param>
        /// <param name="target">The target composition to receive dependencies.</param>
        private static void MergeDependencies(Composition source, Composition target)
        {
            if (source.Dependencies == null || source.Dependencies.Count == 0)
            {
                return;
            }

            // Initialize dependencies collection if null
            target.Dependencies ??= new List<string>();

            // Add only unique dependencies using LINQ
            var newDependencies = source.Dependencies.Where(d => !target.Dependencies.Contains(d));
            target.Dependencies.AddRange(newDependencies);
        }

        #endregion
    }
}
