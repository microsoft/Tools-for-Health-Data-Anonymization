using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class LocalFhirDataLoader : DataLoader<ResourceList>
    {
        public BatchFhirDeIdJobInputData inputData { get; set; }
        protected override async Task LoadDataInternalAsync(Channel<ResourceList> outputChannel, CancellationToken cancellationToken)
        {
            foreach (var item in inputData.sourceDataset)
            {
                var filePath = item["url"];
                List<string> initialData = new List<string>();
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        initialData.Add(reader.ReadLine());
                    }
                }
                var fileName = filePath.Split("\\");
                await outputChannel.Writer.WriteAsync(new ResourceList() { Resources = initialData, inputFileName = filePath, outputFileName = inputData.destinationDataset.folderPath + "\\" + fileName.Last() }, cancellationToken);
            }
            outputChannel.Writer.Complete();
        }
    }
}
