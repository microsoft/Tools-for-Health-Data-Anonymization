// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdBatchHandler
    {
        public IArtifactStore _artifactStore;
        public IQueueClient _client;
        public FhirDeIdBatchHandler(IArtifactStore artifactStore,
            IQueueClient client)
        {
            EnsureArg.IsNotNull(artifactStore, nameof(artifactStore));
            EnsureArg.IsNotNull(client, nameof(client));

            _artifactStore = artifactStore;
            _client = client;
        }

        public async Task<string> ProcessRequestAsync(DeIdConfiguration configuration, BatchDeIdRequestBody inputData)
        {
            var input = new BatchFhirDeIdJobInputData
            {
                DataSourceType = configuration.DataSourceType,
                DataSourceVersion = configuration.DataSourceVersion,
                DeIdConfiguration = configuration,
                SourceDataset = inputData.SourceDataset,
                DestinationDataset = inputData.DestinationDataset,
            };
            var result = await _client.EnqueueAsync(0, new string[] { JsonConvert.SerializeObject(input) }, 0, false, false, new CancellationToken());
            return result.First().Id.ToString();
        }

        public async Task<JobInfo> GetJobStatusById(string id)
        {
            var jobInfo = await _client.GetJobByIdAsync((byte)QueueType.Deid, long.Parse(id), true, new CancellationToken());
            return jobInfo;
        }
    }
}