// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using System.Threading.Channels;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class LocalFhirDataLoader : DataLoader<ResourceList>
    {
        public BatchFhirDeIdJobInputData inputData { get; set; }
        protected override async Task LoadDataInternalAsync(Channel<ResourceList> outputChannel, CancellationToken cancellationToken)
        {

            var filePath = inputData.SourceDataset.URL;
            List<string> initialData = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    initialData.Add(reader.ReadLine());
                }
            }
            var fileName = filePath.Split("\\");
            await outputChannel.Writer.WriteAsync(new ResourceList() { Resources = initialData, inputFileName = filePath, outputFileName = inputData.DestinationDataset.URL + "\\" + fileName.Last() }, cancellationToken);
            
            outputChannel.Writer.Complete();
        }
    }
}
