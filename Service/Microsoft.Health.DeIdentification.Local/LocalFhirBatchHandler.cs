// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Local
{
    public class LocalFhirBatchHandler
    {
        public IArtifactStore _artifactStore;
        public IQueueClient _client;
        public LocalFhirBatchHandler(IArtifactStore artifactStore,
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

        public List<FhirDeIdBatchProcessor> GetFhirDeIdBatchProcessor(DeIdConfiguration configuration)
        {
            var processors = new List<FhirDeIdBatchProcessor>();
            foreach (var modelReference in configuration.ModelConfigReferences) 
            {
                switch (modelReference.ModelType)
                {
                    case DeidModelType.FhirR4PathRuleSet:
                        var configurationContext = _artifactStore.ResolveArtifact<string>(modelReference.ConfigurationLocation);

                        var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromSettingsInJson(configurationContext));
                        processors.Add(new FhirDeIdBatchProcessor((IDeIdOperation<ResourceList, ResourceList>)new FhirPathRuleSetDeIdOperation(engine)));
                        break;
                    default: throw new ArgumentException($"Unsupported model type {modelReference.ModelType}.");
                }
            }
            return processors;
        }
    }
}
