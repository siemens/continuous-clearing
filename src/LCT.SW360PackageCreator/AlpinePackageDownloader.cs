// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// AlpinePackageDownloader class
    /// </summary>
    public class AlpinePackageDownloader : IPackageDownloader
    {

        /// <summary>
        /// Downloads the source package for the specified component and returns the local file path to the downloaded
        /// archive.
        /// </summary>
        /// <param name="component">The component metadata containing information about the package to download. Cannot be null.</param>
        /// <param name="localPathforDownload">The local directory path where the package should be downloaded. Must be a valid, writable path.</param>
        /// <returns>A string containing the full local file path to the downloaded package archive.</returns>
        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            string localPathforSourceRepo = UrlHelper.GetDownloadPathForAlpineRepo();
            var sourceData = component.AlpineSource;
            string sourceCodeDownloadedFolder = GetCurrentDownloadFolderPath(localPathforDownload, component);
            string downloadPath = await DownloadTarFileAndGetPath(component, component.SourceUrl, sourceCodeDownloadedFolder, sourceData, localPathforSourceRepo);

            return downloadPath;

        }

        /// <summary>
        /// copies the build files from source repo to download folder
        /// </summary>
        /// <param name="localPathforDownload"></param>
        /// <param name="component"></param>
        /// <param name="sourceData"></param>
        /// <param name="localPathforSourceRepo"></param>
        private static void CopyBuildFilesFromSourceRepo(string localPathforDownload, ComparisonBomData component, string sourceData, string localPathforSourceRepo)
        {
            if (sourceData != null)
            {
                string[] filenameList = sourceData.Split("\n");
                foreach (var name in filenameList)
                {
                    var fileName = name.Trim();
                    string buildFileLocation = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + component.Name + Dataconstant.ForwardSlash + fileName;
                    if (System.IO.File.Exists(buildFileLocation) && !fileName.EndsWith(".patch"))
                    {

                        string destFile = System.IO.Path.Combine(Directory.CreateDirectory(localPathforDownload + "BuildFiles").ToString(), fileName);
                        System.IO.File.Copy(buildFileLocation, destFile, true);

                    }
                }
            }

        }

        /// <summary>
        /// gets the current download folder path
        /// </summary>
        /// <param name="localPathforDownload"></param>
        /// <param name="component"></param>
        /// <returns>folder path</returns>
        private static string GetCurrentDownloadFolderPath(string localPathforDownload, ComparisonBomData component)
        {
            return $"{localPathforDownload}{component.Name}--{DateTime.Now.ToString("yyyyMMddHHmmss")}{Dataconstant.ForwardSlash}";
        }

        /// <summary>
        /// downloads the tar file and get the path
        /// </summary>
        /// <param name="component"></param>
        /// <param name="SourceUrl"></param>
        /// <param name="localPathforDownload"></param>
        /// <param name="sourceData"></param>
        /// <param name="localPathforSourceRepo"></param>
        /// <returns>file and path</returns>
        private static async Task<string> DownloadTarFileAndGetPath(ComparisonBomData component, string SourceUrl, string localPathforDownload, string sourceData, string localPathforSourceRepo)
        {
            string downloadPath = string.Empty;
            try
            {
                string componenetFullName = GetCorrectFileExtension(SourceUrl);
                string downloadedFilePathWithName = $"{localPathforDownload}{componenetFullName}";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(downloadedFilePathWithName));

                if (!string.IsNullOrEmpty(SourceUrl) && !component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound))
                {
                    Uri uri = new Uri(SourceUrl);
                    downloadPath = await UrlHelper.DownloadFileAsync(uri, downloadedFilePathWithName);
                    ApplyPatchFilesToSourceCode(downloadPath, sourceData, localPathforSourceRepo, component, localPathforDownload);
                }

                CopyBuildFilesFromSourceRepo(localPathforDownload, component, sourceData, localPathforSourceRepo);
                string gZipFilePath = PackageFolderGzip(localPathforDownload, component, downloadPath);
                downloadPath = gZipFilePath;
            }
            catch (WebException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("DownloadTarFileAndGetPath", $"MethodName:DownloadTarFileAndGetPath(), Release Name: {component.Name}@{component.Version}, PackageUrl: {SourceUrl}", ex, "A network error occurred while trying to download the tar file.");
            }
            catch (UriFormatException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("DownloadTarFileAndGetPath", $"MethodName:DownloadTarFileAndGetPath(), Release Name: {component.Name}@{component.Version}, PackageUrl: {SourceUrl}", ex, "The provided URL is not in a valid format.");
            }

            return downloadPath;
        }

        /// <summary>
        /// applies the patch files to source code
        /// </summary>
        /// <param name="downloadPath"></param>
        /// <param name="sourceData"></param>
        /// <param name="localPathforSourceRepo"></param>
        /// <param name="component"></param>
        /// <param name="localPathforDownload"></param>
        private static void ApplyPatchFilesToSourceCode(string downloadPath, string sourceData, string localPathforSourceRepo, ComparisonBomData component, string localPathforDownload)
        {
            string[] buildFilesList = sourceData.Split("\n");
            string sourceCodezippedFolder = string.Empty;

            if (sourceData.Contains(".patch") && (downloadPath.EndsWith(".gz") || downloadPath.EndsWith(".bz2")))
            {
                try
                {
                    PackageFolderUnGzip(localPathforDownload, component, sourceCodezippedFolder, downloadPath);
                    string rootPath = localPathforDownload;
                    string[] directoriesOfSourceFolder = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);
                    sourceCodezippedFolder = directoriesOfSourceFolder[0];
                    if (sourceData.Contains(".patch"))
                    {
                        Process p = new Process();

                        p.StartInfo.RedirectStandardError = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.FileName = Path.Combine(@"git");
                        p.StartInfo.Arguments = "init";
                        p.StartInfo.WorkingDirectory = sourceCodezippedFolder;

                        p.Start();
                        p.WaitForExit();

                    }
                    foreach (var fileNames in buildFilesList)
                    {
                        var fileName = fileNames.Trim();
                        string patchFileFolder = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + component.Name + Dataconstant.ForwardSlash + fileName;
                        if (fileName.Contains(".patch") && File.Exists(patchFileFolder))
                        {
                            ApplyPatchsToSourceCode(patchFileFolder, sourceCodezippedFolder);
                        }
                    }
                    string gitFolderPath = System.IO.Path.Combine(sourceCodezippedFolder, ".git");
                    var directory = new DirectoryInfo(gitFolderPath) { Attributes = FileAttributes.Normal };

                    foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        info.Attributes = FileAttributes.Normal;
                    }

                    directory.Delete(true);

                }
                catch (IOException ex)
                {
                    LogHandlingHelper.ExceptionErrorHandling("ApplyPatchFilesToSourceCode", $"MethodName:ApplyPatchFilesToSourceCode(), ComponentName:{component.Name}, DownloadPath:{downloadPath}", ex, "An I/O error occurred while applying patch files to the source code.");
                }
            }

        }

        /// <summary>
        /// package folder ungzip
        /// </summary>
        /// <param name="localPathforDownload"></param>
        /// <param name="component"></param>
        /// <param name="sourceCodezippedFolder"></param>
        /// <param name="downloadPath"></param>
        public static void PackageFolderUnGzip(string localPathforDownload, ComparisonBomData component, string sourceCodezippedFolder, string downloadPath)
        {
            DirectoryInfo directorySelected = new DirectoryInfo(localPathforDownload);
            if (downloadPath.EndsWith(".bz2"))
            {
                foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.bz2"))
                {
                    string sub = System.IO.Path.Combine(localPathforDownload, fileToDecompress.Name);
                    FileInfo tarFileInfo = new FileInfo(sub);
                    DirectoryInfo targetDirectory = new DirectoryInfo(localPathforDownload);
                    if (!targetDirectory.Exists)
                    {
                        targetDirectory.Create();
                    }
                    using (Stream sourceStream = new BZip2InputStream(tarFileInfo.OpenRead()))
                    {

                        using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(sourceStream, null))
                        {
                            tarArchive.ExtractContents(targetDirectory.FullName);
                        }

                    }
                }
            }
            else
            {
                foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
                {
                    string sub = System.IO.Path.Combine(localPathforDownload, fileToDecompress.Name);
                    FileInfo tarFileInfo = new FileInfo(sub);
                    DirectoryInfo targetDirectory = new DirectoryInfo(localPathforDownload);
                    if (!targetDirectory.Exists)
                    {
                        targetDirectory.Create();
                    }
                    using (Stream sourceStream = new GZipInputStream(tarFileInfo.OpenRead()))
                    {

                        using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(sourceStream, null))
                        {
                            tarArchive.ExtractContents(targetDirectory.FullName);
                        }

                    }
                }
            }

            File.Delete(downloadPath);

        }

        /// <summary>
        /// packages the folder gzip
        /// </summary>
        /// <param name="localPathforDownload"></param>
        /// <param name="component"></param>
        /// <param name="sourceCodezippedFolder"></param>
        /// <returns>package zip folder</returns>
        public static string PackageFolderGzip(string localPathforDownload, ComparisonBomData component, string sourceCodezippedFolder)
        {
            string tarArchivePath;
            if (Directory.GetDirectories(localPathforDownload).Length != 0)
            {

                var tempFolder = Directory.CreateDirectory($"{Directory.GetParent(Directory.GetCurrentDirectory())}" +
                                    $"\\ClearingTool\\DownloadedFiles\\SourceCodeZipped\\{component.Name}\\--" +
                                    $"{DateTime.Now.ToString("yyyyMMddHHmmss")}\\");
                tarArchivePath = tempFolder + (component.Name + "_" + component.Version) + ".tar.gz";
                var InputDirectory = localPathforDownload;
                var OutputFilename = tarArchivePath;
                using Stream zipStream = new FileStream(System.IO.Path.GetFullPath(OutputFilename), FileMode.Create, FileAccess.Write);
                using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
                foreach (var filePath in System.IO.Directory.GetFiles(InputDirectory, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    var relativePath = filePath.Replace(InputDirectory, string.Empty);
                    using Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using Stream fileStreamInZip = archive.CreateEntry(relativePath).Open();
                    fileStream.CopyTo(fileStreamInZip);
                }
            }
            else
            {
                tarArchivePath = sourceCodezippedFolder;
            }

            return tarArchivePath;
        }
        /// <summary>
        /// gets the correct file extension
        /// </summary>
        /// <param name="sourceURL"></param>
        /// <returns>file extension</returns>
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

        /// <summary>
        /// applies the patchs to source code
        /// </summary>
        /// <param name="patchFileFolder"></param>
        /// <param name="sourceCodezippedFolder"></param>
        public static void ApplyPatchsToSourceCode(string patchFileFolder, string sourceCodezippedFolder)
        {
            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = Path.Combine(@"git");
            p.StartInfo.Arguments = $"apply" + " " + patchFileFolder;
            p.StartInfo.WorkingDirectory = sourceCodezippedFolder;

            p.Start();
            p.WaitForExit();
        }
    }
}