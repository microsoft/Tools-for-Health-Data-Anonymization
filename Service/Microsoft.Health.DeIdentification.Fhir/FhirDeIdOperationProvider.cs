// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdOperationProvider : IDeIdOperationProvider
    {
        private IArtifactStore _artifactStore;
        private readonly ILoggerFactory _loggerFactory;
        private ILogger<FhirDeIdOperationProvider> _logger;

        public  FhirDeIdOperationProvider(
            IArtifactStore artifactStore,
            ILoggerFactory loggerFactory,
            ILogger<FhirDeIdOperationProvider> logger)
        {
            _artifactStore = EnsureArg.IsNotNull(artifactStore, nameof(artifactStore));
            _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
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
                        deIdOperations.Add((IDeIdOperation<TSource, TResult>)new FhirPathRuleSetDeIdOperation(engine, _loggerFactory.CreateLogger<FhirPathRuleSetDeIdOperation>()));
                        break;
                    default: throw new ArgumentException($"Unsupported model type {modelReference.ModelType}.");

                }
            }

            return deIdOperations;
        }
    }
}