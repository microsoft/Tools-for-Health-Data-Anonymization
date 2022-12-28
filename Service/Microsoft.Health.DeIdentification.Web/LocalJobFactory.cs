using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Models;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Local;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web
{
    public class LocalJobFactory : IJobFactory
    {
        private LocalFhirDataLoader _fhirDataLoader;
        private LocalFhirBatchHandler _fhirBatchHandler;
        private LocalFhirDataWriter _fhirDataWriter;
        private IDeIdConfigurationStore _deIdConfigurationStore;

        public LocalJobFactory(LocalFhirDataLoader fhirDataLoader,
            LocalFhirBatchHandler fhirBatchHandler,
            LocalFhirDataWriter fhirDataWriter,
            IDeIdConfigurationStore deIdConfiguration)
        { 
            EnsureArg.IsNotNull(fhirDataLoader, nameof(fhirDataLoader));
            EnsureArg.IsNotNull(fhirBatchHandler, nameof(fhirBatchHandler));

            _fhirDataLoader = fhirDataLoader;
            _fhirBatchHandler = fhirBatchHandler;
            _fhirDataWriter = fhirDataWriter;
            _deIdConfigurationStore = deIdConfiguration;
        }
        public IJob Create(JobInfo jobInfo)
        {
            var inputData = JsonConvert.DeserializeObject<BatchInputData>(jobInfo.Definition);
            IJob job;
            switch (inputData.dataSourceType)
            {
                case "fhir":
                    job = new BatchFhirDeIdJob(_fhirDataLoader, _fhirBatchHandler.GetFhirDeIdBatchProcessor(_deIdConfigurationStore.GetByName(inputData.deIdConfiguration)), _fhirDataWriter);
                    break;
                default:
                    throw new InvalidOperationException("Not support DataSourceType");
            }
            return job;
        }
    }
}
