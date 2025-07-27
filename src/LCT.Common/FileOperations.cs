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
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string CatoolBomFilePath { get; set; }
        private readonly IEnvironmentHelper _environmentHelper=new EnvironmentHelper();
        public void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                HandleValidationError("Validation failed for file path.", new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}"), "The provided file path is null, empty, or consists only of whitespace.", _environmentHelper);
            }
            if (!File.Exists(filePath))
            {
                HandleValidationError("File not found at the specified path.", new FileNotFoundException($"The {nameof(filePath)} is not found at this path - {filePath}"), $"Ensure the file exists at the specified path: {filePath}", _environmentHelper);
            }
        }

        /// <summary>
        /// writes the content to the specified file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataToWrite">dataToWrite</param>
        /// <param name="folderPath">folderPath</param>
        /// <param name="fileNameWithExtension">fileNameWithExtension</param>
        /// <param name="projectName">projectName</param>
        public string WriteContentToFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            string fileName = $"{projectName}_{fileNameWithExtension}";
            string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);
            return WriteContentToFileInternal(folderPath, fileName, jsonString, "WriteContentToFile");
        }
        public string WriteContentToOutputBomFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            string fileName = $"{projectName}_{fileNameWithExtension}";
            string content = dataToWrite.ToString();
            return WriteContentToFileInternal(folderPath, fileName, content, "WriteContentToOutputBomFile");
        }

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
                    List<Component> list = new List<Component>(comparisonData.Components.Count + components.Components.Count);
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
                }
                else
                {
                    LogHandlingHelper.BasicErrorHandling("Combine components in already existing Bomfile", "CombineComponentsFromExistingBOM()", "Invalid path entered. Please check if the comparison BOM path entered is correct.", "Ensure the file path is correct and the file exists.");
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
        public string WriteContentToCycloneDXFile<T>(T dataToWrite, string filePath, string fileNameWithExtension)
        {
            try
            {
                Logger.Debug($"WriteContentToCycloneDXFile():Starting to write content to CycloneDX file. FolderPath: {filePath}, FileName: {fileNameWithExtension}");
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

        private static void BackupTheGivenFile(string folderPath, string fileName)
        {
            string oldFile = Path.Combine(folderPath, fileName);
            string newFile = string.Format("{0}/{1:MM-dd-yyyy_HHmm_ss}_{2}_{3}", folderPath, DateTime.Now, FileConstant.backUpKey, fileName);
            Logger.Debug($"BackupTheGivenFile():start backup for oldFile{oldFile},newFile{newFile}");
            try
            {
                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile);
                    Logger.Debug($"BackupTheGivenFile():Successfully created backup of {oldFile} at {newFile}");
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Environment.ExitCode = -1;
            }
            catch (NotSupportedException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Environment.ExitCode = -1;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Backup BOM File", "BackupTheGivenFile()", ex, $"OldFile: {oldFile}, NewFile: {newFile}");
                Environment.ExitCode = -1;
            }
        }

        public string WriteContentToReportNotApprovedFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string name)
        {
            string fileName = $"{name}_{fileNameWithExtension}";
            string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return WriteContentToFileInternal(folderPath, fileName, jsonString, "WriteContentToReportNotApprovedFile");
        }
        public string WriteContentToMultipleVersionsFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            string fileName = $"{projectName}_{fileNameWithExtension}";
            string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return WriteContentToFileInternal(folderPath, fileName, jsonString, "WriteContentToMultipleVersionsFile");
        }
        private static string WriteContentToFileInternal(string folderPath, string fileNameWithExtension, string content, string operationName)
        {
            try
            {
                Logger.Debug($"{operationName}(): Starting to write content to file. FolderPath: {folderPath}, FileName: {fileNameWithExtension}");

                string filePath = Path.Combine(folderPath, fileNameWithExtension);
                Logger.Debug($"Generated FilePath with filename for writing data: {filePath}");

                BackupTheGivenFile(folderPath, fileNameWithExtension);
                File.WriteAllText(filePath, content);

                Logger.Debug($"{operationName}(): Content successfully written to file.");
                return "success";
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", $"{operationName}()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", $"{operationName}()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", $"{operationName}()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
        }
        private static void HandleValidationError(string message, Exception exception, string additionalDetails,IEnvironmentHelper environmentHelper)
        {
            LogHandlingHelper.ExceptionErrorHandling("Validation Error", message, exception, additionalDetails);
            environmentHelper.CallEnvironmentExit(-1);
        }
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

        private static Composition FindMatchingComposition(List<Composition> compositions, Composition sourceComposition)
        {
            return compositions.FirstOrDefault(c =>
                c.Assemblies != null &&
                sourceComposition.Assemblies != null &&
                c.Assemblies.SequenceEqual(sourceComposition.Assemblies));
        }

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
    }
}
