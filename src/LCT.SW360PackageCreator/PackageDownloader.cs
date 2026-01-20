// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace LCT.SW360PackageCreator
{
    public class PackageDownloader : IPackageDownloader
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<DownloadedSourceInfo> m_downloadedSourceInfos = new List<DownloadedSourceInfo>();
        private const string Source = "source";
        private static readonly string[] WindowsLineSeparators = ["\r\n"];
        private static readonly string[] UnixLineSeparators = ["\n"];

        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            Logger.DebugFormat("DownloadPackage():Start downloading process of source code for this component , Name-{0},version-{1}", component.Name, component.Version);
            string path = Download(component, localPathforDownload);
            Logger.DebugFormat("DownloadPackage():Completed downloading process of source code for this component , Name-{0},version-{1}", component.Name, component.Version);
            await Task.Delay(10);
            return path;
        }

        private string Download(ComparisonBomData component, string downloadPath)
        {
            string downloadedPackageName = string.Empty;
            string taggedVersion = GetCorrectVersion(component);
            if (string.IsNullOrEmpty(taggedVersion))
            {
                return string.Empty;
            }
            if (CheckIfAlreadyDownloaded(component, taggedVersion, out string alreadyDownloadedPath))
            {
                return alreadyDownloadedPath;
            }

            string sourceUrl = component.SourceUrl.TrimEndOfString("/");
            string fileName = CommonHelper.GetSubstringOfLastOccurance(sourceUrl, "/");
            string safeTaggedVersion = SanitizeFileName(taggedVersion);
            string cloneFolderName = $"{fileName}-{safeTaggedVersion}";
            string compressedFilePath = $"{downloadPath}{fileName}-{safeTaggedVersion}-{Source}";
            compressedFilePath = $"{compressedFilePath}{FileConstant.TargzFileExtension}";
            downloadPath = $"{downloadPath}{cloneFolderName}/";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Download", $"MethodName:Download(), Release Name: {component.Name}@{component.Version}, DownloadPath: {downloadPath}", ex, "Unauthorized access occurred while trying to create the download directory.");
                return downloadedPackageName;
            }
            Result result = CloneSource(component, downloadPath, taggedVersion, compressedFilePath);

            Logger.DebugFormat("DownloadSourceCodeUsingGitClone:Release Name : {0}@{1}, stdout:{2}, npm pack stdErr:{3}", component.Name, component.Version, result?.StdOut, result?.StdErr);
            m_downloadedSourceInfos.Add(new DownloadedSourceInfo() { Name = component.Name, Version = component.Version, DownloadedPath = compressedFilePath, SourceRepoUrl = component.DownloadUrl, TaggedVersion = taggedVersion });
            component.DownloadUrl = GetSourceRepositoryUrl(component, taggedVersion);
            return compressedFilePath;
        }
        private static string GetSourceRepositoryUrl(ComparisonBomData component, string tag)
        {
            if (!string.IsNullOrEmpty(component.SourceUrl) && component.SourceUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(tag))
            {
                string repoUrl = component.SourceUrl;
                repoUrl = repoUrl.TrimEnd('/');
                string encodedTag = Uri.EscapeDataString(tag);
                return $"{repoUrl}/tree/{encodedTag}";
            }
            return component.DownloadUrl;
        }
        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            name = name.Replace('/', '_').Replace('\\', '_');
            return name;
        }
        private static string GetCorrectVersion(ComparisonBomData component)
        {
            Logger.DebugFormat("GetCorrectVersion():Start identifying correct version for this component , Name-{0},version-{1}", component.Name, component.Version);
            string correctVersion = string.Empty;
            Result result = ListTagsOfComponent(component);

            string[] taglist = GetTagListFromResult(result);
            string baseVersion = GetBaseVersion(component.Version);

            foreach (string item in taglist)
            {
                Logger.DebugFormat("GetCorrectVersion - Current Item:{0}", item);

                if (!string.IsNullOrWhiteSpace(item))
                {
                    string tag = item[(item.IndexOf("tags/") + 5)..];
                    Logger.DebugFormat("baseobject - {0},tag -{1}", item, tag);

                    if (tag.Contains(component.Version, StringComparison.OrdinalIgnoreCase) &&
                tag.Contains(component.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return tag;
                    }
                }
            }
            foreach (string item in taglist)
            {
                Logger.DebugFormat("GetCorrectVersion - Current Item:{0}", item);

                if (!string.IsNullOrWhiteSpace(item))
                {
                    string tag = item[(item.IndexOf("tags/") + 5)..];
                    Logger.DebugFormat("baseobject - {0},tag -{1}", item, tag);

                    if (tag.Contains(component.Version))
                    {
                        return tag;
                    }
                    else if (tag.Contains(baseVersion))
                    {
                        return tag;
                    }
                }
            }
            Logger.DebugFormat("GetCorrectVersion():Completed identifying correct version for this component ,given version:{0}, correct Version:{1}", component.Version, correctVersion);
            return correctVersion;
        }
        private static string[] GetTagListFromResult(Result result)
        {
            if (result == null || string.IsNullOrEmpty(result.StdOut))
            {
                return [];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return result.StdOut.Split(WindowsLineSeparators, StringSplitOptions.None);
            }
            else
            {
                return result.StdOut.Split(UnixLineSeparators, StringSplitOptions.None);
            }
        }
        private static string GetBaseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return version;
            }  
            if (Version.TryParse(version, out var parsed))
            {
                if (parsed.Build == 0 && (parsed.Revision == -1 || parsed.Revision == 0))
                {
                    return $"{parsed.Major}.{parsed.Minor}";
                } 
                return version;
            }
            return version;
        }
        private bool CheckIfAlreadyDownloaded(ComparisonBomData component, string tagVersion, out string downloadedPath)
        {
            downloadedPath = string.Empty;
            if (m_downloadedSourceInfos.Exists(x => x.TaggedVersion == tagVersion && x.SourceRepoUrl == component.DownloadUrl))
            {
                downloadedPath = m_downloadedSourceInfos
                    .Where(x => x.TaggedVersion == tagVersion && x.SourceRepoUrl == component.DownloadUrl)
                    .Select(x => x.DownloadedPath)
                    .Single();
                return true;
            }

            return false;
        }

        private static Result ListTagsOfComponent(ComparisonBomData component)
        {
            Logger.DebugFormat("ListTagsOfComponent():Start git process for identifying list of tags for this component , Name-{0},version-{1}", component.Name, component.Version);
            string gitCommand = $"ls-remote --tags {component.DownloadUrl}";
            Logger.DebugFormat("ListTagsOfComponent():{0}@{1} --> {2}", component.Name, component.Version, gitCommand);

            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = Path.Combine(@"git");
            p.StartInfo.Arguments = gitCommand;

            const int timeOutMs = 200 * 60 * 1000;
            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo, timeOutMs);
            Result result = processResult?.Result ?? new Result();
            Logger.DebugFormat("ListTagsOfComponent():{0}:{1}, output:{2}, Error:{3}", gitCommand, result.ExitCode, result.StdOut, result.StdErr);
            Logger.DebugFormat("ListTagsOfComponent():Completed git process for identifying list of tags for this component , Name-{0},version-{1}", component.Name, component.Version);
            return result;
        }

        private static Result CloneSource(ComparisonBomData component, string downloadPath, string taggedVersion, string compressedFilePath)
        {
            const int timeoutInMs = 200 * 60 * 1000;
            List<string> gitCommands = GetGitCloneCommands(component, taggedVersion, compressedFilePath);
            Result result = null;

            Logger.DebugFormat("CloneSource:Download Path : {0}  Taggedversion:{1}", downloadPath, taggedVersion);

            foreach (string command in gitCommands)
            {
                Process p = new Process();
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = Path.Combine(@"git");
                p.StartInfo.Arguments = command;
                p.StartInfo.WorkingDirectory = downloadPath;

                var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo, timeoutInMs);
                result = processResult?.Result;
                Logger.DebugFormat("CloneSource:Command : {0}, ExitCode:{1},stdout:{2}, npm pack stdErr:{3}", command, result?.ExitCode, result?.StdOut, result?.StdErr);
            }

            return result;
        }

        private static List<string> GetGitCloneCommands(ComparisonBomData component, string taggedVersion, string compressedFilePath)
        {
            return new List<string>()
           {
               $"init .",
               $"remote add origin {component.DownloadUrl}",
               $"config --local --add core.autocrlf false",
               $"config --local --add core.eol lf",
               $"fetch --prune --progress --depth=1 origin refs/tags/{taggedVersion}",
               $"archive --format=tar.gz --output={compressedFilePath} FETCH_HEAD"
           };
        }
        
    }
}
