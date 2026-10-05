// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class CycleTestWorker
    {
        private const string PipeVariable = "DICOM_CYCLE_TEST_PIPE";
        private const string ScenarioVariable = "DICOM_CYCLE_TEST_SCENARIO";
        private const string WorkerName = "Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.CycleTestWorker.GivenWorkerRequest_WhenInvokedInChildTestHost_RunsIsolatedAssertions";
        private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

        [Fact]
        public void GivenWorkerRequest_WhenInvokedInChildTestHost_RunsIsolatedAssertions()
        {
            var pipeName = Environment.GetEnvironmentVariable(PipeVariable);
            if (pipeName == null)
            {
                return;
            }

            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            pipe.Connect((int)StartupTimeout.TotalMilliseconds);
            using var writer = new StreamWriter(pipe) { AutoFlush = true };
            void Signal(string message) => writer.WriteLineAsync(message).WaitAsync(ExecutionTimeout).GetAwaiter().GetResult();
            Signal(WorkerName + ":" + Environment.ProcessId);
            var scenario = Environment.GetEnvironmentVariable(ScenarioVariable);
            if (scenario == "startup-hang")
            {
                Thread.Sleep(Timeout.Infinite);
            }

            if (scenario == "delayed-start")
            {
                Thread.Sleep(ExecutionTimeout + TimeSpan.FromSeconds(1));
            }

            if (scenario == "cycle")
            {
                var mode = Environment.GetEnvironmentVariable("DICOM_CYCLE_TEST_MODE") ?? throw new InvalidOperationException("Cycle mode is required.");
                Assert.Contains(mode, new[] { "dataset", "inplace", "clone" });
                bool indirect = bool.Parse(Environment.GetEnvironmentVariable("DICOM_CYCLE_TEST_INDIRECT") ?? throw new InvalidOperationException("Cycle shape is required."));
                AnonymizerPreflightTests.AssertCyclicInputRejected(mode, indirect, () => Signal("started"));
            }
            else
            {
                Signal("started");
                if (scenario == "execution-hang")
                {
                    Thread.Sleep(Timeout.Infinite);
                }

                Assert.Contains(scenario, new[] { "delayed-start", "assertion-failure" });
                Assert.False(scenario == "assertion-failure", "Synthetic worker assertion failure.");
            }

            Signal("completed");
        }

        [Fact]
        public async Task GivenDelayedWorkerStartup_WhenRunning_ExecutionDeadlineStartsAtInvocation()
        {
            await RunAsync("delayed-start");
        }

        [Theory]
        [InlineData("startup-hang", "startup")]
        [InlineData("execution-hang", "execution")]
        public async Task GivenHungWorker_WhenDeadlineExpires_OwnedProcessIsTerminated(string scenario, string phase)
        {
            var error = await Assert.ThrowsAsync<TimeoutException>(() => RunAsync(scenario));
            Assert.Contains(phase, error.Message);
        }

        [Fact]
        public async Task GivenWorkerAssertionFailure_WhenRunning_FailureIsNotReportedAsSuccess()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync("assertion-failure"));
        }

        internal static async Task RunAsync(string scenario, string mode = "dataset", bool indirect = false)
        {
            if (Environment.GetEnvironmentVariable(PipeVariable) != null)
            {
                throw new InvalidOperationException("An isolated cycle worker must not spawn another worker.");
            }

            var pipeName = "dicom-cycle-" + Guid.NewGuid().ToString("N");
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add("vstest");
            start.ArgumentList.Add(typeof(CycleTestWorker).Assembly.Location);
            start.ArgumentList.Add("--TestCaseFilter:FullyQualifiedName=" + WorkerName);
            start.Environment[PipeVariable] = pipeName;
            start.Environment[ScenarioVariable] = scenario;
            start.Environment["DICOM_CYCLE_TEST_MODE"] = mode;
            start.Environment["DICOM_CYCLE_TEST_INDIRECT"] = indirect.ToString();
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Cycle test worker could not start.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var reader = new StreamReader(pipe);
            Process? worker = null;
            try
            {
                using var startup = new CancellationTokenSource(StartupTimeout);
                try
                {
                    await pipe.WaitForConnectionAsync(startup.Token);
                    var identity = await reader.ReadLineAsync(startup.Token);
                    if (identity == null || !identity.StartsWith(WorkerName + ":", StringComparison.Ordinal) ||
                        !int.TryParse(identity.Substring(WorkerName.Length + 1), out var workerId))
                    {
                        throw new InvalidOperationException("The exact cycle worker case did not identify itself.");
                    }

                    worker = Process.GetProcessById(workerId);

                    // The startup-hang control uses a short deadline only after the host connects.
                    if (scenario == "startup-hang")
                    {
                        startup.CancelAfter(TimeSpan.FromSeconds(1));
                    }

                    if (await reader.ReadLineAsync(startup.Token) != "started")
                    {
                        throw new InvalidOperationException("Cycle test worker exited without starting the invocation.");
                    }
                }
                catch (OperationCanceledException) when (startup.IsCancellationRequested)
                {
                    throw new TimeoutException("Cycle test worker exceeded its startup deadline.");
                }

                using var execution = new CancellationTokenSource(ExecutionTimeout);
                try
                {
                    if (await reader.ReadLineAsync(execution.Token) != "completed")
                    {
                        throw new InvalidOperationException("Cycle test worker failed its assertions before completing.");
                    }
                }
                catch (OperationCanceledException) when (execution.IsCancellationRequested)
                {
                    throw new TimeoutException("Cycle test worker exceeded its five-second execution deadline.");
                }

                await process.WaitForExitAsync().WaitAsync(ShutdownTimeout);
                var output = await Task.WhenAll(stdout, stderr).WaitAsync(ShutdownTimeout);
                Assert.True(process.ExitCode == 0, string.Concat(output));
            }
            finally
            {
                try
                {
                    if (worker != null && !worker.HasExited)
                    {
                        worker.Kill(entireProcessTree: true);
                    }
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }

                    await process.WaitForExitAsync().WaitAsync(ShutdownTimeout);
                    if (worker != null)
                    {
                        using (worker)
                        {
                            await worker.WaitForExitAsync().WaitAsync(ShutdownTimeout);
                            Assert.True(worker.HasExited, "Child test host was not reaped.");
                        }
                    }

                    await Task.WhenAll(stdout, stderr).WaitAsync(ShutdownTimeout);
                    Assert.True(process.HasExited, "Cycle test launcher was not reaped.");
                }
            }
        }
    }
}
