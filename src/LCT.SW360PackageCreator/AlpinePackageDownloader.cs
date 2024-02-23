// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// AlpinePackageDownloader class
    /// </summary>
    public class AlpinePackageDownloader : IPackageDownloader
    {

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            string localPathforSourceRepo = UrlHelper.GetDownloadPathForAlpineRepo();
            var sourceData = component.AlpineSource;
            string sourceCodeDownloadedFolder = GetCurrentDownloadFolderPath(localPathforDownload, component);
            string downloadPath = await DownloadTarFileAndGetPath(component, component.SourceUrl, sourceCodeDownloadedFolder, sourceData, localPathforSourceRepo);

            return downloadPath;

        }

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

        private static string GetCurrentDownloadFolderPath(string localPathforDownload, ComparisonBomData component)
        {
            return $"{localPathforDownload}{component.Name}--{DateTime.Now.ToString("yyyyMMddHHmmss")}{Dataconstant.ForwardSlash}";
        }


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
                Logger.Debug($"DownloadTarFileAndGetPath :WebException :Release Name : {component.Name}@{component.Version}-PackageUrl: ,Error {ex}");
            }
            catch (UriFormatException ex)
            {
                Logger.Debug($"DownloadTarFileAndGetPath:Release Name : {component.Name}@{component.Version}: Error {ex}");
            }

            return downloadPath;
        }
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
                        if (fileName.Contains(".patch")&& File.Exists(patchFileFolder))
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
                    Logger.Debug(ex.ToString());
                }
            }

        }


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

        public static string PackageFolderGzip(string localPathforDownload, ComparisonBomData component, string sourceCodezippedFolder)
        {
            string tarArchivePath;
            if (Directory.GetDirectories(localPathforDownload).Length != 0)
            {

                var tempFolder = Directory.CreateDirectory($"{Directory.GetParent(Directory.GetCurrentDirectory())}\\ClearingTool\\DownloadedFiles\\SourceCodeZipped\\{component.Name}\\--{DateTime.Now.ToString("yyyyMMddHHmmss")}\\");
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

        private static void ApplyPatchsToSourceCode(string patchFileFolder, string sourceCodezippedFolder)
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