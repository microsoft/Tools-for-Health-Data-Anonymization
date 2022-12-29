// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class BatchFhirDeIdJob : IJob
    {
        private LocalFhirDataLoader _dataLoader;
        private List<FhirDeIdBatchProcessor> _batchProcessors;
        private LocalFhirDataWriter _dataWriter;

        public BatchFhirDeIdJob(LocalFhirDataLoader fhirDataLoader, 
            List<FhirDeIdBatchProcessor> fhirDeIdBatchProcessors, 
            LocalFhirDataWriter fhirDataWriter)
        {
            EnsureArg.IsNotNull(fhirDataLoader, nameof(fhirDataLoader));
            EnsureArg.IsNotNull(fhirDeIdBatchProcessors, nameof(fhirDeIdBatchProcessors));
            EnsureArg.IsNotNull(fhirDataWriter, nameof(fhirDataWriter));

            _dataLoader = fhirDataLoader;
            _batchProcessors = fhirDeIdBatchProcessors;
            _dataWriter = fhirDataWriter;
        }
        public async Task<string> ExecuteAsync(JobInfo jobInfo, IProgress<string> progress, CancellationToken cancellationToken)
        {
            _dataLoader.inputData = JsonConvert.DeserializeObject<BatchFhirDeIdJobInputData>(jobInfo.Definition);
            BatchFhirDeIdJobResult currentResult = string.IsNullOrEmpty(jobInfo.Result) ? new BatchFhirDeIdJobResult() : JsonConvert.DeserializeObject<BatchFhirDeIdJobResult>(jobInfo.Result);

            (Channel<ResourceList> inputChannel, Task loadTask) = _dataLoader.Load(cancellationToken);
            List<Channel<ResourceList>> innerChannels = new List<Channel<ResourceList>>();
            List<Task> innerTasks = new List<Task>();
            foreach (var processor in _batchProcessors)
            {
                (Channel<ResourceList> currentChannel, Task currentTask) = processor.Process(innerChannels.Count < 1 ? inputChannel : innerChannels.Last(), cancellationToken);

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
                
                currentResult.Outputs.Add(batchProgress);
                progress.Report(JsonConvert.SerializeObject(batchProgress));
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
            return JsonConvert.SerializeObject(currentResult);
        }
    }
}
