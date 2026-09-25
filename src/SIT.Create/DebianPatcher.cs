// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Model;
using SIT.Create.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SIT.Create
{
    [ExcludeFromCodeCoverage]
    public class DebianPatcher : IDebianPatcher
    {

        /// <summary>
        /// Apply Patch
        /// </summary>
        /// <param name="component"></param>
        /// <param name="localDownloadPath"></param>
        /// <param name="fileName"></param>
        /// <returns>result</returns>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const int TimeoutInMs = 200 * 60 * 1000;
        const string DpkgSourceExecutable = "dpkg-source";
        const string TarExecutable = "tar";

        public Result ApplyPatch(ComparisonBomData component, string localDownloadPath, string fileName)
        {
            Logger.DebugFormat("ApplyPatch():Started Applying patch for component, Name-{0},version-{1}", component.Name, component.Version);

            localDownloadPath = localDownloadPath.Substring(0, localDownloadPath.Length - 1);
            string combinedFileName = $"{component.Name}_{component.Version.Replace(".debian", "")}{FileConstant.DebianCombinedPatchExtension}";
            string archiveDirName = component.Name;

            Result result = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = RunDockerCommand(localDownloadPath, "dpkg-source", "-x", "--", fileName, archiveDirName);

                if (result != null && result.ExitCode == 0)
                {
                    result = RunDockerCommand(localDownloadPath, "tar", "--create", "--bzip2", "--force-local", "--format=gnu", "--sort=name", "--owner=0", "--group=0", "--numeric-owner", "--mtime=2020-01-01 00:00:00Z", $"--file={combinedFileName}", "--", $"{archiveDirName}/");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = RunLocalCommand(DpkgSourceExecutable, localDownloadPath, "-x", "--", fileName, archiveDirName);

                if (result != null && result.ExitCode == 0)
                {
                    result = RunLocalCommand(TarExecutable, localDownloadPath, "--create", "--bzip2", "--force-local", "--format=gnu", "--sort=name", "--owner=0", "--group=0", "--numeric-owner", "--mtime=2020-01-01 00:00:00Z", $"--file={combinedFileName}", "--", $"{archiveDirName}/");
                }
            }
            else
            {
                Logger.Warn("ApplyPatch():Unsupported operating system detected. Debian patch application was skipped.");
            }

            Logger.DebugFormat("ApplyPatch():Completed Applying patch for component, Name-{0},version-{1}", component.Name, component.Version);
            return result;
        }

        static Result RunLocalCommand(string fileName, string workingDirectory, params string[] arguments)
        {
            ProcessStartInfo startInfo = CreateStartInfo(fileName, workingDirectory);
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return ProcessAsyncHelper.RunAsync(startInfo, TimeoutInMs)?.Result;
        }

        static Result RunDockerCommand(string localDownloadPath, string executable, params string[] arguments)
        {
            ProcessStartInfo startInfo = CreateStartInfo("docker");
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("-w");
            startInfo.ArgumentList.Add(FileConstant.ContainerDir);
            startInfo.ArgumentList.Add("--rm");
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add($"{localDownloadPath}:{FileConstant.ContainerDir}");
            startInfo.ArgumentList.Add(FileConstant.DockerImage);
            startInfo.ArgumentList.Add(executable);

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return ProcessAsyncHelper.RunAsync(startInfo, TimeoutInMs)?.Result;
        }

        static ProcessStartInfo CreateStartInfo(string fileName, string workingDirectory = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            return startInfo;
        }
    }
}
