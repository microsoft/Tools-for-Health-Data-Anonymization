// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.DeIdentification.Local;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Web
{
    public class LocalJobFactory : IJobFactory
    {
        private LocalFhirDataLoader _fhirDataLoader;
        private LocalFhirBatchHandler _fhirBatchHandler;
        private LocalFhirDataWriter _fhirDataWriter;
        private IDeIdConfigurationRegistration _deIdConfigurationStore;

        public LocalJobFactory(LocalFhirDataLoader fhirDataLoader,
            LocalFhirBatchHandler fhirBatchHandler,
            LocalFhirDataWriter fhirDataWriter,
            IDeIdConfigurationRegistration deIdConfiguration)
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
            var inputData = JsonConvert.DeserializeObject<BatchFhirDeIdJobInputData>(jobInfo.Definition);
            
            IJob job;
            switch (inputData.DataSourceType)
            {
                case DataSourceType.Fhir:
                    job = new BatchFhirDeIdJob(_fhirDataLoader, _fhirBatchHandler.GetFhirDeIdBatchProcessor(inputData.DeIdConfiguration), _fhirDataWriter);
                    break;
                default:
                    throw new InvalidOperationException("Not support DataSourceType");
            }
            return job;
        }
    }
}
