using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirDataLoader : DataLoader<string>
    {
        public BatchFhirDeIdJobInputData inputData { get; set; }
        protected override async Task LoadDataInternalAsync(Channel<string> outputChannel, CancellationToken cancellationToken)
        {
            foreach ( var requestContext in inputData.sourceDataset)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestContext["url"]);
                var response = await new HttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false); 
                var context = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                await outputChannel.Writer.WriteAsync(context, cancellationToken);
            }
        }
    }
}
