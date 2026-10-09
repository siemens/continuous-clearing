// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SIT.Common.UTest
{
    [TestFixture]
    public class ProcessAsyncHelperTest
    {
        private static ProcessStartInfo BuildEchoStartInfo(string text)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new ProcessStartInfo("cmd.exe", $"/c echo {text}")
                : new ProcessStartInfo("/bin/sh", $"-c \"echo {text}\"");
        }

        private static ProcessStartInfo BuildSleepStartInfo(int seconds)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new ProcessStartInfo("cmd.exe", $"/c ping 127.0.0.1 -n {seconds + 1} > nul")
                : new ProcessStartInfo("/bin/sh", $"-c \"sleep {seconds}\"");
        }

        [Test]
        public async Task RunAsync_WithRedirectedOutput_CapturesStdOutAndExitsSuccessfully()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("hello-world");
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Act
            Result result = await ProcessAsyncHelper.RunAsync(startInfo);

            // Assert
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("hello-world"));
        }

        [Test]
        public async Task RunAsync_WithoutRedirectedStreams_StillReturnsExitCode()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("no-redirect");
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = false;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Act
            Result result = await ProcessAsyncHelper.RunAsync(startInfo);

            // Assert
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Is.Empty);
            Assert.That(result.StdErr, Is.Empty);
        }

        [Test]
        public async Task RunAsync_WithTimeoutShorterThanProcessDuration_KillsProcessAndReturnsExitCode()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildSleepStartInfo(30);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Act
            Result result = await ProcessAsyncHelper.RunAsync(startInfo, 500);

            // Assert
            Assert.That(result.ExitCode, Is.Not.Null);
        }

        [Test]
        public async Task RunAsync_WithNullTimeout_WaitsForProcessCompletion()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("no-timeout");
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Act
            Result result = await ProcessAsyncHelper.RunAsync(startInfo, null);

            // Assert
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("no-timeout"));
        }

        [Test]
        public void ReadOutputStreamProcess_WithRedirectedStreams_DoesNotThrow()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("redirect-check");
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            // Act & Assert
            Assert.DoesNotThrow(() => ProcessAsyncHelper.ReadOutputStreamProcess(process));

            process.WaitForExit();
        }

        [Test]
        public void ReadOutputStreamProcess_WithoutRedirectedStreams_DoesNotThrow()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("no-redirect-check");
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = false;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            // Act & Assert
            Assert.DoesNotThrow(() => ProcessAsyncHelper.ReadOutputStreamProcess(process));

            process.WaitForExit();
        }

        [Test]
        public void STDOutHandler_WithRedirectedOutput_AddsTaskToProcessTasks()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("stdout-handler");
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            using Process process = new Process { StartInfo = startInfo };
            var processTasks = new List<Task>();

            // Act
            var builder = ProcessAsyncHelper.STDOutHandler(process, processTasks);

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(processTasks, Has.Count.EqualTo(1));
        }

        [Test]
        public void STDOutHandler_WithoutRedirectedOutput_DoesNotAddTask()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("stdout-handler-skip");
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = false;
            using Process process = new Process { StartInfo = startInfo };
            var processTasks = new List<Task>();

            // Act
            var builder = ProcessAsyncHelper.STDOutHandler(process, processTasks);

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(processTasks, Is.Empty);
        }

        [Test]
        public void STDErrorHandler_WithRedirectedError_AddsTaskToProcessTasks()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("stderr-handler");
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            using Process process = new Process { StartInfo = startInfo };
            var processTasks = new List<Task>();

            // Act
            var builder = ProcessAsyncHelper.STDErrorHandler(process, processTasks);

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(processTasks, Has.Count.EqualTo(1));
        }

        [Test]
        public void STDErrorHandler_WithoutRedirectedError_DoesNotAddTask()
        {
            // Arrange
            ProcessStartInfo startInfo = BuildEchoStartInfo("stderr-handler-skip");
            startInfo.RedirectStandardError = false;
            startInfo.UseShellExecute = false;
            using Process process = new Process { StartInfo = startInfo };
            var processTasks = new List<Task>();

            // Act
            var builder = ProcessAsyncHelper.STDErrorHandler(process, processTasks);

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(processTasks, Is.Empty);
        }
    }
}
