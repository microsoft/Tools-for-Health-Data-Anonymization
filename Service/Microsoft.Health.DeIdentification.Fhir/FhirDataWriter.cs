// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDataWriter : DataWriter<ResourceList, ResourceList>
    {
        public override ResourceList[] BatchProcessFunc(BatchInput<ResourceList> input)
        {
            return input.Sources.ToArray();
        }

        protected override Task CommitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
