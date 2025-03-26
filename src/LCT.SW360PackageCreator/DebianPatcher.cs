// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace LCT.SW360PackageCreator
{
    [ExcludeFromCodeCoverage]
    public class DebianPatcher : IDebianPatcher
    {
        public Result ApplyPatch(ComparisonBomData component, string localDownloadPath, string fileName)
        {
            Result result;
            string dockerCommandForApplyPatching;
            localDownloadPath = localDownloadPath.Substring(0, localDownloadPath.Length - 1);
            string combinedFileName = $"{component.Name}_{component.Version.Replace(".debian", "")}{FileConstant.DebianCombinedPatchExtension}";
            const string tarParameters = "--force-local --format=gnu --sort=name --owner=0 --group=0 --numeric-owner --mtime=\'2020-01-01 00:00:00Z\'";
            string archiveDirName = $"{component.Name}";

            const int timeoutInMs = 200 * 60 * 1000;
            using (Process p = new Process())
            {
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dockerCommandForApplyPatching = $"docker run -w {FileConstant.ContainerDir} --rm -v \"{localDownloadPath}\":" +
                    $"{FileConstant.ContainerDir} {FileConstant.DockerImage} /bin/bash -c \" dpkg-source -x {fileName} {archiveDirName}; " +
                    $"tar -cjf {combinedFileName} {archiveDirName}/ {tarParameters}\"";
                    p.StartInfo.FileName = Path.Combine("cmd.exe");
                    p.StartInfo.Arguments = @"/c " + dockerCommandForApplyPatching;
                }
                else
                {
                    dockerCommandForApplyPatching = $"cd {localDownloadPath}; dpkg-source -x {fileName} {archiveDirName}; " +
                    $"tar -cjf {combinedFileName} {archiveDirName}/ {tarParameters}";
                    p.StartInfo.FileName = FileConstant.DockerCMDTool;
                    p.StartInfo.Arguments = "-c \" " + dockerCommandForApplyPatching + " \"";
                }
                // Run as administrator
                p.StartInfo.Verb = "runas";

                var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo, timeoutInMs);
                result = processResult?.Result;
            }
            return result;
        }
    }
}
