// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDeIdHandler
    {
        private readonly int outputChannelLimit = 1000;
        private readonly int maxRunningOperationCount = 5;
        private const int MaxContextCount = 700;
        private IDeIdOperationProvider _deIdOperationProvider;

        public FhirDeIdHandler(IDeIdOperationProvider deIdOperationProvider)
        {
            _deIdOperationProvider = EnsureArg.IsNotNull(deIdOperationProvider, nameof(deIdOperationProvider));
        }

        public async Task<ResourceList> ProcessRequestAsync(DeIdConfiguration config, ResourceList resourceList)
        {
            if (resourceList.Resources.Count >= MaxContextCount)
            {
                throw new Exception($"Context count can't be greater than {MaxContextCount}.");

            }

            var operations = _deIdOperationProvider.CreateDeIdOperations<ResourceList, ResourceList>(config);
            foreach (var operation in operations)
            {
                resourceList = operation.Process(resourceList);

            }

            return resourceList;

        }

    }
}
