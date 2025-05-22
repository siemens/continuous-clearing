// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
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

        public void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHandlingHelper.ExceptionErrorHandling("Validation Error",$"Validation failed for file path.",new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}"),"The provided file path is null, empty, or consists only of whitespace.");
                throw new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}");
            }

            if (!File.Exists(filePath))
            {
                LogHandlingHelper.ExceptionErrorHandling("Validation Error",$"File not found at the specified path.",new FileNotFoundException($"The {nameof(filePath)} is not found at this path - {filePath}"),$"Ensure the file exists at the specified path: {filePath}");
                throw new FileNotFoundException($"The {nameof(filePath)}  is not found at this path" +
                    $" - {filePath}");
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
            try
            {
                Logger.Debug($"WriteContentToFile():Starting to write content to file. FolderPath: {folderPath}, FileName: {fileNameWithExtension}, ProjectName: {projectName}");

                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);

                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"Generated FilePath: {filePath}");

                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, jsonString);
                Logger.Debug("WriteContentToBomFile():Content successfully written to file.");
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToFile():Completed writing content to the file.\n");
            return "success";

        }
        public string WriteContentToOutputBomFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            try
            {
                Logger.Debug($"WriteContentToBomFile():Starting to write BOM content to file. FolderPath: {folderPath}, FileName: {fileNameWithExtension}, ProjectName: {projectName}");

                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = CatoolBomFilePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"Generated FilePath: {filePath}");

                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, dataToWrite.ToString());
                Logger.Debug("WriteContentToBomFile():Content successfully written to file.");
            }
            catch (IOException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            catch (SecurityException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("FileOperations", "WriteContentToBomFile()", e, $"FolderPath: {folderPath}, FileName: {fileNameWithExtension}");
                return "failure";
            }
            Logger.Debug($"WriteContentToBomFile():Completed writing content to the file.");
            return "success";

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
            string newFile = string.Format("{0}/{1:MM-dd-yyyy_HHmm_ss}_Backup_{2}", folderPath, DateTime.Now, fileName);
            Logger.Debug($"BackupTheGivenFile():Starting backup process. OldFile: {oldFile}, NewFile: {newFile}");
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
            try
            {
                Logger.Debug($"WriteContentToReportNotApprovedFile():Starting to write content to Report Not Approved file. FolderPath: {folderPath}, FileName: {fileNameWithExtension}, Name: {name}");
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{name}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"Generated FilePath: {filePath}");
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
        public string WriteContentToMultipleVersionsFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            try
            {
                Logger.Debug($"WriteContentToMultipleVersionsFile():Starting to write content to Multiple Versions file. FolderPath: {folderPath}, FileName: {fileNameWithExtension}, ProjectName: {projectName}");
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"Generated FilePath: {filePath}");
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
    }
}
