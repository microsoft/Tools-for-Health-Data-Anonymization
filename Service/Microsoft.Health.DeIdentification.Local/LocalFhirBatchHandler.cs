using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Models;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<List<JobInfo>> ProcessRequestAsync(DeIdConfiguration configuration, BatchInputData inputData)
        {
            var processors = GetFhirDeIdBatchProcessor(configuration);
            var result = await _client.EnqueueAsync(0, new string[] { JsonConvert.SerializeObject(inputData) }, 0, false, false, new CancellationToken());
            return result.ToList();
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
