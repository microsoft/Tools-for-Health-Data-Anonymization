using EnsureThat;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        private IArtifactStore _artifactStore;

        public  FhirDeIdOperationProvider(IArtifactStore artifactStore)
        {
            _artifactStore = EnsureArg.IsNotNull(artifactStore, nameof(artifactStore));
        }

        public IList<IDeIdOperation<TSource, TResult>> CreateDeIdOperations<TSource, TResult>(DeIdConfiguration deIdConfiguration)
        {
            EnsureArg.IsNotNull(deIdConfiguration, nameof(deIdConfiguration));
            var deIdOperations = new List<IDeIdOperation<TSource, TResult>>();
            foreach (var modelReference in deIdConfiguration.ModelConfigReferences)
            {
                switch (modelReference.ModelType)
                {
                    case DeidModelType.FhirR4PathRuleSet:
                        var configurationContent = _artifactStore.ResolveArtifact<string>(modelReference.ConfigurationLocation);

                        var engine = new AnonymizerEngine(AnonymizerConfigurationManager.CreateFromSettingsInJson(configurationContent));
                        deIdOperations.Add((IDeIdOperation<TSource, TResult>)new FhirPathRuleSetDeIdOperation(engine));
                        break;

                }
            }

            return deIdOperations;
        }
    }
}