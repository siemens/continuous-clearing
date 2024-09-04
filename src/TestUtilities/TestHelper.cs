// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
            proc.StartInfo.FileName = OutFolder + @"\PackageIdentifier.exe";
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.Start();
            // To avoid deadlocks, always read the output stream first and then wait. 
            proc.StandardOutput.ReadToEnd();


#if DEBUG
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
#endif
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                BOMCreated = true;
            }
            if (proc.ExitCode < 0) {
                Console.WriteLine(proc.StartInfo.FileName);
                Console.WriteLine(proc.StartInfo.Arguments);
            }

                return proc.ExitCode;
        }

        public static int RunComponentCreatorExe(string[] args)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = OutFolder + @"\SW360PackageCreator.exe";
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            proc.Start();
            proc.StandardOutput.ReadToEnd();
#if DEBUG
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
#endif
            proc.WaitForExit();
            if (proc.ExitCode < 0)
            {
                Console.WriteLine(proc.StartInfo.FileName);
                Console.WriteLine(proc.StartInfo.Arguments);
            }
            return proc.ExitCode;
        }

        public static int RunArtifactoryUploaderExe(string[] args)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = OutFolder + @"\ArtifactoryUploader.exe";
            proc.StartInfo.Arguments = GetArguments(args);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            proc.Start();
            proc.StandardOutput.ReadToEnd();
#if DEBUG
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
#endif
            proc.WaitForExit();
            if (proc.ExitCode < 0)
            {
                Console.WriteLine(proc.StartInfo.FileName);
                Console.WriteLine(proc.StartInfo.Arguments);
            }
            return proc.ExitCode;
        }

        private static string GetArguments(string[] args)
        {
            StringBuilder argumenstList = new StringBuilder();
            int i = 0;
            foreach (var arg in args)
            {
                argumenstList.Append(args[i] + " ");
                i++;
            }
            return argumenstList.ToString();
        }

    }
}
