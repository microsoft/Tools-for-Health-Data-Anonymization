// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Fhir.Model;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class BatchFhirDeIdJob : IJob
    {
        private readonly BatchFhirDeIdJobInputData _input;
        private BatchFhirDeIdJobResult _result;

        private readonly LocalFhirDataLoader _dataLoader;
        private readonly List<FhirDeIdBatchProcessor> _batchProcessors;
        private readonly LocalFhirDataWriter _dataWriter;

        private readonly ILogger<BatchFhirDeIdJob> _logger;

        public BatchFhirDeIdJob(
            BatchFhirDeIdJobInputData input,
            BatchFhirDeIdJobResult result,
            LocalFhirDataLoader fhirDataLoader, 
            List<FhirDeIdBatchProcessor> fhirDeIdBatchProcessors, 
            LocalFhirDataWriter fhirDataWriter,
            ILogger<BatchFhirDeIdJob> logger)
        {
            _input = EnsureArg.IsNotNull(input, nameof(input));
            _result = EnsureArg.IsNotNull(result, nameof(result));

            _dataLoader = EnsureArg.IsNotNull(fhirDataLoader, nameof(fhirDataLoader));
            _batchProcessors = EnsureArg.IsNotNull(fhirDeIdBatchProcessors, nameof(fhirDeIdBatchProcessors));
            _dataWriter = EnsureArg.IsNotNull(fhirDataWriter, nameof(fhirDataWriter));

            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }
        public async Task<string> ExecuteAsync(JobInfo jobInfo, IProgress<string> progress, CancellationToken cancellationToken)
        {
            _dataLoader.inputData = _input;

            (Channel<BatchFhirDataContext> inputChannel, Task loadTask) = _dataLoader.Load(cancellationToken);
            List<Channel<BatchFhirDataContext>> innerChannels = new List<Channel<BatchFhirDataContext>>();
            List<Task> innerTasks = new List<Task>();
            foreach (var processor in _batchProcessors)
            {
                (Channel<BatchFhirDataContext> currentChannel, Task currentTask) = processor.Process(innerChannels.Count < 1 ? inputChannel : innerChannels.Last(), cancellationToken);

                innerChannels.Add(currentChannel);
                innerTasks.Add(currentTask);
            }

            (Channel<OutputInfo> outputChannel, Task writeTask) = _dataWriter.Process(innerChannels.Last(), cancellationToken);

            await foreach (OutputInfo batchProgress in outputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                
                _result.Outputs.Add(batchProgress);
                progress.Report(JsonConvert.SerializeObject(_result));
            }

            try
            {
                await writeTask;
                foreach (var task in innerTasks)
                {
                    await task;
                }
                await loadTask;
            }
            catch
            {
                throw;
            }

            _result.Metadata.FileCount = _result.Outputs.Count;
            _result.Metadata.CompletedTime = DateTimeOffset.UtcNow;
            _result.Metadata.ExecutionTimeInMS = (_result.Metadata.CompletedTime - _result.Metadata.StartTime).TotalMilliseconds;

            return JsonConvert.SerializeObject(_result);
        }
    }
}
