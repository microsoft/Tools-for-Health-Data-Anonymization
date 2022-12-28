using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Fhir.Local
{
    public class LocalFhirDataWriter : DataWriter<ResourceList, OutputInfo>
    {
        private readonly ILogger<LocalFhirDataWriter> _logger;

        public LocalFhirDataWriter(ILogger<LocalFhirDataWriter> logger)
        {
            _logger = logger;
        }

        public BatchFhirDeIdJobInputData inputData { get; set; }
        public BatchFhirDeIdJobResult jobResult { get; set; }
        public override OutputInfo[] BatchProcessFunc(BatchInput<ResourceList> input)
        {
            var result = new List<OutputInfo>();
            foreach (var item in input.Sources)
            {
                File.AppendAllText(item.outputFileName, JsonConvert.SerializeObject(item.Resources));
                result.Add(new OutputInfo() { outputUrl = item.outputFileName, sourceUrl = item.inputFileName});
            }
            return result.ToArray();
        }

        protected override async Task CommitAsync(CancellationToken cancellationToken)
        {
            // TODO commit 
            _logger.LogInformation("commit local fhir data write");

            await Task.Delay(1000);
        }
    }
}
