// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Local;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Web
{
    public class BatchJobFactory : IJobFactory
    {
        private LocalFhirDataLoader _fhirDataLoader;
        private LocalFhirDataWriter _fhirDataWriter;
        private IDeIdOperationProvider _deIdOperationProvider;

        public BatchJobFactory(LocalFhirDataLoader fhirDataLoader,
            LocalFhirDataWriter fhirDataWriter,
            IDeIdOperationProvider deIdOperationProvider)
        {
            _fhirDataLoader = EnsureArg.IsNotNull(fhirDataLoader, nameof(fhirDataLoader));

            _fhirDataWriter = EnsureArg.IsNotNull(fhirDataWriter, nameof(fhirDataWriter));
            _deIdOperationProvider = EnsureArg.IsNotNull(deIdOperationProvider, nameof(deIdOperationProvider));
        }
        public IJob Create(JobInfo jobInfo)
        {
            var inputData = JsonConvert.DeserializeObject<BatchFhirDeIdJobInputData>(jobInfo.Definition);
            
            IJob job;
            switch (inputData.DataSourceType)
            {
                case DataSourceType.Fhir:

                    var operations = _deIdOperationProvider.CreateDeIdOperations<StringBatchData, StringBatchData>(inputData.DeIdConfiguration);

                    var processors = operations.Select(operation => new FhirDeIdBatchProcessor(operation)).ToList();

                    job = new BatchFhirDeIdJob(_fhirDataLoader, processors, _fhirDataWriter);
                    break;
                default:
                    throw new InvalidOperationException("Not support DataSourceType");
            }
            return job;
        }
    }
}
