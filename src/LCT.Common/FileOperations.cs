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

        #endregion

        #region Properties

        public static string CatoolBomFilePath { get; set; }

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
                throw new ArgumentException($"Invalid value for the {nameof(filePath)} - {filePath}");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The {nameof(filePath)}  is not found at this path" +
                    $" - {filePath}");
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
                Logger.Debug($"WriteContentToFile():folderpath-{folderPath},fileNameWithExtension-{fileNameWithExtension}," + $"projectName-{projectName}");

                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);

                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"filePath-{filePath}");

                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, jsonString);

            }
            catch (IOException e)
            {
                Logger.Debug($"WriteContentToFile():Error:", e);
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug($"WriteContentToFile():Error:", e);
                return "failure";
            }
            catch (SecurityException e)
            {
                Logger.Debug($"WriteContentToFile():Error:", e);
                return "failure";
            }
            Logger.Debug($"WriteContentToFile():End");
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
        public string WriteContentToOutputBomFile<T>(T dataToWrite, string folderPath, string fileNameWithExtension, string projectName)
        {
            try
            {
                Logger.Debug($"WriteContentToBomFile():folderpath-{folderPath},fileNameWithExtension-{fileNameWithExtension}," + $"projectName-{projectName}");

                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = CatoolBomFilePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"filePath-{filePath}");

                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, dataToWrite.ToString());

            }
            catch (IOException e)
            {
                Logger.Debug($"WriteContentToBomFile():Error:", e);
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug($"WriteContentToBomFile():Error:", e);
                return "failure";
            }
            catch (SecurityException e)
            {
                Logger.Debug($"WriteContentToBomFile():Error:", e);
                return "failure";
            }
            Logger.Debug($"WriteContentToBomFile():End");
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
                    Logger.Error($"Error:Invalid path entered,Please check if the comparison BOM  path entered is correct");
                    throw new FileNotFoundException();
                }
            }
            catch (IOException e)
            {
                Environment.ExitCode = -1;
                Logger.Error($"Error:Invalid path entered,Please check if the comparison BOM  path entered is correct", e);
            }
            catch (UnauthorizedAccessException e)
            {
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
                Logger.Debug($"WriteContentToCycloneDXFile():folderpath-{filePath}");
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented);
                string filename = Path.GetFileName(fileNameWithExtension);
                filePath = $"{filePath}\\{filename}";
                File.Copy(fileNameWithExtension, filePath);
                File.WriteAllText(filePath, jsonString);


            }
            catch (IOException e)
            {
                Logger.Debug($"WriteContentToCycloneDXFile():Error:", e);
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug($"WriteContentToCycloneDXFile():Error:", e);
                return "failure";
            }
            catch (SecurityException e)
            {
                Logger.Debug($"WriteContentToCycloneDXFile():Error:", e);
                return "failure";
            }
            Logger.Debug($"WriteContentToCycloneDXFile():End");
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
            Logger.Debug($"BackupTheGivenFile():oldFile{oldFile},newFile{newFile}");
            try
            {
                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile);
                    Logger.Debug($"BackupTheGivenFile():Successfully taken backup of {oldFile}");
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"Error occurred while generating backup BOM file", ex);
                Environment.ExitCode = -1;
            }
            catch (NotSupportedException ex)
            {
                Logger.Error($"Error occurred while generating backup BOM file", ex);
                Environment.ExitCode = -1;
            }
            catch (UnauthorizedAccessException ex)
            {
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
                Logger.Debug($"WriteContentToReportNotApprovedFile():folderpath-{folderPath},fileNameWithExtension-{fileNameWithExtension}," +
                    $"Name-{name}");
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{name}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"filePath-{filePath}");
                File.WriteAllText(filePath, jsonString);

            }
            catch (IOException e)
            {
                Logger.Debug($"WriteContentToReportNotApprovedFile():Error:", e);
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug($"WriteContentToReportNotApprovedFile():Error:", e);
                return "failure";
            }
            catch (SecurityException e)
            {
                Logger.Debug($"WriteContentToReportNotApprovedFile():Error:", e);
                return "failure";
            }
            Logger.Debug($"WriteContentToReportNotApprovedFile():End");
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
                Logger.Debug($"WriteContentToMultipleVersionsFile():folderpath-{folderPath},fileNameWithExtension-{fileNameWithExtension}," +
                    $"projectName-{projectName}");
                string jsonString = JsonConvert.SerializeObject(dataToWrite, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string fileName = $"{projectName}_{fileNameWithExtension}";

                string filePath = Path.Combine(folderPath, fileName);
                Logger.Debug($"filePath-{filePath}");
                BackupTheGivenFile(folderPath, fileName);
                File.WriteAllText(filePath, jsonString);

            }
            catch (IOException e)
            {
                Logger.Debug($"WriteContentToMultipleVersionsFile():Error:", e);
                return "failure";
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug($"WriteContentToMultipleVersionsFile():Error:", e);
                return "failure";
            }
            catch (SecurityException e)
            {
                Logger.Debug($"WriteContentToMultipleVersionsFile():Error:", e);
                return "failure";
            }
            Logger.Debug($"WriteContentToMultipleVersionsFile():End");
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
