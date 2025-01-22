// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// the DebianPackageDownloader class
    /// </summary>
    public class DebianPackageDownloader : IPackageDownloader
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly IDebianPatcher _debianPatcher;

        public DebianPackageDownloader(IDebianPatcher debianPatcher)
        {
            _debianPatcher = debianPatcher;
        }

        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            string downloadPath = string.Empty;
            string CurrentDownloadFolder = GetCurrentDownloadFolderPath(localPathforDownload, component);

            if (component.PatchURls != null)
            {
                string patchedFolderPath = string.Empty;
                Dictionary<string, string> fileInfo = await GetFileDetails(component, CurrentDownloadFolder);

                if (fileInfo.ContainsKey("DSCFILE") && fileInfo["IsAllFileDownloaded"] == "YES")
                {
                    patchedFolderPath = ApplyPatchforComponents(component, CurrentDownloadFolder, fileInfo["DSCFILE"]);
                }
                else
                {
                    Logger.Debug($"DownloadComponentPackage:Failed to download All files for : {component.Name}@{component.Version}");
                }

                try
                {
                    if (!string.IsNullOrEmpty(patchedFolderPath) && File.Exists(patchedFolderPath))
                    {
                        downloadPath = patchedFolderPath;
                    }
                }
                catch (DirectoryNotFoundException ex)
                {
                    Logger.Debug($"DownloadComponentPackage:DirectoryNotFoundException : Release Name : {component.Name}@{component.Version},Error {ex}");
                }
                catch (IOException ex)
                {
                    Logger.Debug($"DownloadComponentPackage:IOException:Release Name : {component.Name}@{component.Version},Error {ex}");
                }
            }
            else
            {
                downloadPath = await DownloadTarFileAndGetPath(component, component.SourceUrl, CurrentDownloadFolder);
            }

            if (string.IsNullOrEmpty(downloadPath))
            {
                Logger.Error($"Failed to download source for {component.Name}-{component.Version}");
            }
            return downloadPath;
        }

        private static async Task<Dictionary<string, string>> GetFileDetails(ComparisonBomData component, string currentDownloadFolder)
        {
            Dictionary<string, string> fileInfo = new Dictionary<string, string>();
            bool IsAllFileDownloaded = true;

            foreach (string path in component.PatchURls)
            {
                try
                {
                    string file = await DownloadTarFileAndGetPath(component, path, currentDownloadFolder);

                    if (string.IsNullOrEmpty(file))
                    {
                        IsAllFileDownloaded = false;
                    }

                    if (!string.IsNullOrEmpty(file) && file.Contains(FileConstant.DSCFileExtension) && !fileInfo.ContainsKey("DSCFILE"))
                    {
                        fileInfo.Add("DSCFILE", Path.GetFileName(file));
                    }
                }
                catch (ArgumentException ex)
                {
                    Logger.Debug($"GetFileDetails:Release Name : {component.Name}@{component.Version}: Error {ex}");
                }
            }

            if (IsAllFileDownloaded)
            {
                fileInfo.Add("IsAllFileDownloaded", "YES");
            }
            else
            {
                fileInfo.Add("IsAllFileDownloaded", "NO");
            }

            return fileInfo;
        }

        private static string GetCorrectFileExtension(string sourceURL)
        {
            int idx = sourceURL.LastIndexOf(Dataconstant.ForwardSlash);
            string fullname = string.Empty;

            if (idx != -1)
            {
                fullname = sourceURL.Substring(idx + 1);
            }

            return fullname;
        }

        private static string GetCurrentDownloadFolderPath(string localPathforDownload, ComparisonBomData component)
        {
            return $"{localPathforDownload}{component.Name}--{DateTime.Now.ToString("yyyyMMddHHmmss")}{Dataconstant.ForwardSlash}";
        }

        private static async Task<string> DownloadTarFileAndGetPath(ComparisonBomData component, string SourceUrl, string localPathforDownload)
        {
            string downloadPath = string.Empty;
            try
            {
                string componenetFullName = GetCorrectFileExtension(SourceUrl);
                string downloadFilePath = $"{localPathforDownload}{componenetFullName}";
                Directory.CreateDirectory(Path.GetDirectoryName(downloadFilePath));

                if (!string.IsNullOrEmpty(SourceUrl) && !component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound))
                {
                    Uri uri = new Uri(SourceUrl);
                    downloadPath = await UrlHelper.DownloadFileAsync(uri, downloadFilePath);
                }
            }
            catch (WebException ex)
            {
                Logger.Debug($"DownloadTarFileAndGetPath :WebException :Release Name : {component.Name}@{component.Version}-PackageUrl: ,Error {ex}");
            }
            catch (UriFormatException ex)
            {
                Logger.Debug($"DownloadTarFileAndGetPath:Release Name : {component.Name}@{component.Version}: Error {ex}");
            }

            return downloadPath;
        }

        public string ApplyPatchforComponents(ComparisonBomData component, string localDownloadPath, string fileName)     
        {
            Result result;
            string patchedFile = string.Empty;
            result = _debianPatcher.ApplyPatch(component, localDownloadPath, fileName);

            if (result != null)
            {
                if (result.ExitCode != 0)
                {
                    Logger.Debug($"ApplyPatch:File Name : {fileName},Error {result.StdErr}");
                    Logger.Debug($"ApplyPatch:File Name : {fileName},Retrying......");
                    DeletePatchedFolderAndFile($"{localDownloadPath}/patchedfiles", patchedFile);
                    // Waiting for 2 seconds before retrying.
                    Thread.Sleep(2000);
                    patchedFile = GetPatchedFilePathByRetrying(localDownloadPath, component, fileName);
                }
                else
                {
                    patchedFile = GetPatchedFileFromDownloadedFolder(localDownloadPath);
                }
            }
            else
            {
                Logger.Debug($"ApplyPatch:File Name : {fileName},Error {"Timeout happend while applying patch!"}");
                Logger.Debug($"ApplyPatch:File Name : {fileName},Retrying......");
                DeletePatchedFolderAndFile($"{localDownloadPath}/patchedfiles", patchedFile);
                // Waiting for 2 seconds before retrying.
                Thread.Sleep(2000);
                patchedFile = GetPatchedFilePathByRetrying(localDownloadPath, component, fileName);
            }

            return patchedFile;
        }

        private string GetPatchedFilePathByRetrying(string currentDownloadFolder, ComparisonBomData component, string dscFileName)
        {
            string patchedFilePath = string.Empty;
            Result result;
            result = _debianPatcher.ApplyPatch(component, currentDownloadFolder, dscFileName);

            if (result != null)
            {
                if (result.ExitCode == 0)
                {
                    Logger.Debug($"GetPatchedFilePathByRetrying:File Name : {dscFileName},Success in retry.");
                    patchedFilePath = GetPatchedFileFromDownloadedFolder(currentDownloadFolder);
                }
                else
                {
                    Logger.Debug($"GetPatchedFilePathByRetrying:File Name : {dscFileName},Failure in retry.");
                    Logger.Debug($"GetPatchedFilePathByRetrying:File Name : {dscFileName},Error {result.StdErr}");
                }
            }
            else
            {
                Logger.Debug($"GetPatchedFilePathByRetrying:File Name : {dscFileName},Error {"Timeout happend while applying patch!"}");
            }

            return patchedFilePath;
        }

        private static void DeletePatchedFolderAndFile(string folderPath, string downloadPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    Logger.Debug($"DeletePatchedFolder : Folder Name : {Path.GetDirectoryName(folderPath)}, {"Success!!"}");
                }
            }
            catch (IOException ex)
            {
                Logger.Debug($"DeletePatchedFolder : Folder Name : {Path.GetDirectoryName(folderPath)},Error {ex}");
            }
            try
            {
                if (File.Exists(downloadPath))
                {
                    File.Delete(downloadPath);
                    Logger.Debug($"DeletePatchedFile : File Name : {Path.GetFileName(downloadPath)}, {"Success!!"}");
                }
            }
            catch (IOException ex)
            {
                Logger.Debug($"DeletePatchedFile:File Name : {Path.GetFileName(downloadPath)},Error {ex}");
            }
        }

        public static string GetPatchedFileFromDownloadedFolder(string currentDownloadFolder)
        {
            string patchedFilePath = string.Empty;
            try
            {
                DirectoryInfo rootDir = new DirectoryInfo(currentDownloadFolder);
                FileInfo[] filesInDir = rootDir.GetFiles("*" + FileConstant.DebianCombinedPatchExtension);

                if (filesInDir.Length > 0)
                {
                    patchedFilePath = filesInDir[0].FullName;
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Debug($"GetPatchedFileFromDownloadedFolder:DownloadPath : {currentDownloadFolder},Error {ex}");
            }
            return patchedFilePath;
        }

    }
}
