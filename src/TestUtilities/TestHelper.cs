// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public static class TestHelper
    {
        static TestHelper()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            OutFolder = Path.GetDirectoryName(exePath);
        }

        public static string OutFolder { get; private set; }

        public static bool BOMCreated { get; private set; }

        public static int RunBOMCreatorExe(string[] args)
        {
            Process proc = new Process();
            string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "PackageIdentifier.exe" : "PackageIdentifier";
            proc.StartInfo.FileName = Path.Combine(OutFolder, executableName);
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.StartInfo.CreateNoWindow = true;

            // Capture and print the output in real-time
            proc.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDOUT] " + e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDERR] " + e.Data); };
            
            proc.Start();

            // Start reading the output asynchronously
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                BOMCreated = true;
            }

            if (proc.ExitCode < 0)
            {
                Console.WriteLine("Executable Path: " + proc.StartInfo.FileName);
                Console.WriteLine("Arguments: " + proc.StartInfo.Arguments);
            }

            return proc.ExitCode;
        }

        public static int RunComponentCreatorExe(string[] args)
        {
            Process proc = new Process();
            string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SW360PackageCreator.exe" : "SW360PackageCreator";
            proc.StartInfo.FileName = Path.Combine(OutFolder, executableName);
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            proc.StartInfo.CreateNoWindow = true;

            // Capture and print the output in real-time
            proc.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDOUT] " + e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDERR] " + e.Data); };

            proc.Start();

            // Start reading the output asynchronously
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            if (proc.ExitCode < 0)
            {
                Console.WriteLine("Executable Path: " + proc.StartInfo.FileName);
                Console.WriteLine("Arguments: " + proc.StartInfo.Arguments);
            }

            return proc.ExitCode;
        }

        public static int RunArtifactoryUploaderExe(string[] args)
        {
            Process proc = new Process();
            string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ArtifactoryUploader.exe" : "ArtifactoryUploader";
            proc.StartInfo.FileName = Path.Combine(OutFolder, executableName);
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            proc.StartInfo.CreateNoWindow = true;

            // Capture and print the output in real-time
            proc.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDOUT] " + e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[STDERR] " + e.Data); };

            proc.Start();

            // Start reading the output asynchronously
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            if (proc.ExitCode < 0)
            {
                Console.WriteLine("Executable Path: " + proc.StartInfo.FileName);
                Console.WriteLine("Arguments: " + proc.StartInfo.Arguments);
            }

            return proc.ExitCode;
        }

        private static string GetArguments(string[] args)
        {
            StringBuilder argumentsList = new StringBuilder();

            foreach (var arg in args)
            {
                // Enclose arguments with spaces in quotes
                if (arg.Contains(" "))
                {
                    argumentsList.Append($"\"{arg}\" ");
                }
                else
                {
                    argumentsList.Append(arg + " ");
                }
            }

            return argumentsList.ToString().TrimEnd(); // Trim trailing space
        }

    }
}