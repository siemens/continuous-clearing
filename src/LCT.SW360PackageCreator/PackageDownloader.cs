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
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<DownloadedSourceInfo> m_downloadedSourceInfos = new List<DownloadedSourceInfo>();
        private const string Source = "source";

        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            string path = Download(component, localPathforDownload);
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
                Logger.Debug($"DownloadSourceCodeUsingGitClone():{ex}");
                return downloadedPackageName;
            }           
            Result result = CloneSource(component, downloadPath, taggedVersion, compressedFilePath);

            Logger.Debug($"DownloadSourceCodeUsingGitClone:Release Name : {component.Name}@{component.Version}, stdout:{result?.StdOut}, npm pack stdErr:{result?.StdErr}");
            m_downloadedSourceInfos.Add(new DownloadedSourceInfo() { Name = component.Name, Version = component.Version, DownloadedPath = compressedFilePath, SourceRepoUrl = component.DownloadUrl, TaggedVersion = taggedVersion });
            component.DownloadUrl = GetSourceRepositoryUrl(component,taggedVersion);
            return compressedFilePath;
        }
        private static string GetSourceRepositoryUrl(ComparisonBomData component, string tag)
        {            
            if (!string.IsNullOrEmpty(component.SourceUrl) && component.SourceUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(tag))
            {
                string repoUrl =component.SourceUrl;
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
            string correctVersion = string.Empty;
            Result result = ListTagsOfComponent(component);

            string[] taglist;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                taglist = result?.StdOut?.Split("\r\n") ?? Array.Empty<string>();
            }
            else
            {
                taglist = result?.StdOut?.Split("\n") ?? Array.Empty<string>();
            }
            string baseVersion = GetBaseVersion(component.Version);

            foreach (string item in taglist)
            {
                Logger.Debug($"GetCorrectVersion - Current Item:{item}");

                if (!string.IsNullOrWhiteSpace(item))
                {
                    string tag = item[(item.IndexOf("tags/") + 5)..];
                    Logger.Debug($"baseobject - {item},tag -{tag}");

                    if (tag.Contains(component.Version, StringComparison.OrdinalIgnoreCase) &&
                tag.Contains(component.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return tag;
                    }
                }
            }
            foreach (string item in taglist)
            {
                Logger.Debug($"GetCorrectVersion - Current Item:{item}");

                if (!string.IsNullOrWhiteSpace(item))
                {
                    string tag = item[(item.IndexOf("tags/") + 5)..];
                    Logger.Debug($"baseobject - {item},tag -{tag}");

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
            Logger.Debug($"componentName - given version:{component.Version}, correctVersion:{correctVersion}");
            return correctVersion;
        }       
        
        private static string GetBaseVersion(string version)
        {
            try
            {
                var parsedVersion = Version.Parse(version);
                if (parsedVersion.Build == 0 && parsedVersion.Revision == -1)
                {
                    return $"{parsedVersion.Major}.{parsedVersion.Minor}";
                }
                return version;
            }
            catch (FormatException)
            {
                Logger.Debug($"Invalid version format: {version}");
                return version;
            }
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
            string gitCommand = $"ls-remote --tags {component.DownloadUrl}";
            Logger.Debug($"GetCorrectVersion():{component.Name}@{component.Version} --> {gitCommand}");

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
            Logger.Debug($"GetCorrectVersion:{gitCommand}:{result.ExitCode}, output:{result.StdOut}, Error:{result.StdErr}");
            return result;
        }

        private static Result CloneSource(ComparisonBomData component, string downloadPath, string taggedVersion, string compressedFilePath)
        {
            const int timeoutInMs = 200 * 60 * 1000;
            List<string> gitCommands = GetGitCloneCommands(component, taggedVersion, compressedFilePath);
            Result result = null;

            Logger.Debug($"CloneSource:Download Path : {downloadPath}  Taggedversion:{taggedVersion}");

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
                Logger.Debug($"CloneSource:Command : {command}, ExitCode:{result?.ExitCode},stdout:{result?.StdOut}, npm pack stdErr:{result?.StdErr}");
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
