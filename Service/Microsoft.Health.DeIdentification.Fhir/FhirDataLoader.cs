// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDataLoader : DataLoader<string>
    {
        public BatchFhirDeIdJobInputData inputData { get; set; }
        protected override async Task LoadDataInternalAsync(Channel<string> outputChannel, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, inputData.SourceDataset.URL);
            var response = await new HttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false); 
            var context = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            await outputChannel.Writer.WriteAsync(context, cancellationToken);
        }
    }
}
