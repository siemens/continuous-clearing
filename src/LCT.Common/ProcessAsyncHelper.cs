// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common
{
    /// <summary>
    /// Process helper with asynchronous interface
    /// </summary>
    public static class ProcessAsyncHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Run a process asynchronously
        /// <para>To capture STDOUT, set StartInfo.RedirectStandardOutput to TRUE</para>
        /// <para>To capture STDERR, set StartInfo.RedirectStandardError to TRUE</para>
        /// </summary>
        /// <param name="startInfo">ProcessStartInfo object</param>
        /// <param name="timeoutMs">The timeout in milliseconds (null for no timeout)</param>
        /// <returns>Result object</returns>
        public static async Task<Result> RunAsync(ProcessStartInfo startInfo, int? timeoutMs = 5 * 60 * 1000)
        {
            Result result = new Result();

            using (var process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true })
            {
                // List of tasks to wait for a whole process exit
                List<Task> processTasks = new List<Task>();

                // === EXITED Event handling ===
                var processExitEvent = new TaskCompletionSource<object>();
                process.Exited += (sender, args) =>
                {
                    processExitEvent.TrySetResult(true);
                };
                processTasks.Add(processExitEvent.Task);

                // === STDOUT handling ===
                var stdOutBuilder = STDOutHandler(process, processTasks);
                // === STDERR handling ===

                var stdErrBuilder = STDErrorHandler(process, processTasks);

                // === START OF PROCESS ===
                if (!process.Start())
                {
                    result.ExitCode = process.ExitCode;
                    return result;
                }

                // Reads the output stream first as needed and then waits because deadlocks are possible
                ReadOutputStreamProcess(process);

                // === ASYNC WAIT OF PROCESS ===

                // Process completion = exit AND stdout (if defined) AND stderr (if defined)
                Task processCompletionTask = Task.WhenAll(processTasks);

                // Task to wait for exit OR timeout (if defined)
                Task<Task> awaitingTask = timeoutMs.HasValue
                    ? Task.WhenAny(Task.Delay(timeoutMs.Value), processCompletionTask)
                    : Task.WhenAny(processCompletionTask);

                // Let's now wait for something to end...

                try
                {
                    if ((await awaitingTask.ConfigureAwait(false)) != processCompletionTask)
                    {
                        process.Kill();
                    }
                    result.ExitCode = process.ExitCode;
                }
                catch (AggregateException ex)
                {
                    Logger.Error($"Exception in RunAsync method(){ex}");
                }

                // Read stdout/stderr
                result.StdOut = stdOutBuilder.ToString();
                result.StdErr = stdErrBuilder.ToString();
            }

            return result;
        }
        public static void ReadOutputStreamProcess(Process process)
        {
            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }
            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }
        }
        public static StringBuilder STDOutHandler(Process process, List<Task> processTasks)
        {
            var stdOutBuilder = new StringBuilder();
            if (process.StartInfo.RedirectStandardOutput)
            {
                var stdOutCloseEvent = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                    {
                        stdOutCloseEvent.TrySetResult(true);
                    }
                    else
                    {
                        stdOutBuilder.AppendLine(e.Data);
                    }
                };

                processTasks.Add(stdOutCloseEvent.Task);
            }
            return stdOutBuilder;

        }
        public static StringBuilder STDErrorHandler(Process process, List<Task> processTasks)
        {
            var stdErrBuilder = new StringBuilder();
            if (process.StartInfo.RedirectStandardError)
            {
                var stdErrCloseEvent = new TaskCompletionSource<bool>();

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                    {
                        stdErrCloseEvent.TrySetResult(true);
                    }
                    else
                    {
                        stdErrBuilder.AppendLine(e.Data);
                    }
                };

                processTasks.Add(stdErrCloseEvent.Task);
            }
            return stdErrBuilder;

        }

    }

}
