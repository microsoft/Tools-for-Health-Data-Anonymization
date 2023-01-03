// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
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
        private readonly LocalFhirDataLoader _fhirDataLoader;
        private readonly LocalFhirDataWriter _fhirDataWriter;
        private readonly IDeIdOperationProvider _deIdOperationProvider;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<BatchJobFactory> _logger;

        public BatchJobFactory(LocalFhirDataLoader fhirDataLoader,
            LocalFhirDataWriter fhirDataWriter,
            IDeIdOperationProvider deIdOperationProvider,
            ILoggerFactory loggerFactory,
            ILogger<BatchJobFactory> logger)
        {
            _fhirDataLoader = EnsureArg.IsNotNull(fhirDataLoader, nameof(fhirDataLoader));

            _fhirDataWriter = EnsureArg.IsNotNull(fhirDataWriter, nameof(fhirDataWriter));
            _deIdOperationProvider = EnsureArg.IsNotNull(deIdOperationProvider, nameof(deIdOperationProvider));

            _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }
        public IJob Create(JobInfo jobInfo)
        {
            var inputData = JsonConvert.DeserializeObject<BatchFhirDeIdJobInputData>(jobInfo.Definition);
            
            IJob job;
            switch (inputData.DataSourceType)
            {
                case DataSourceType.Fhir:

                    BatchFhirDeIdJobResult result = string.IsNullOrWhiteSpace(jobInfo.Result)
                        ? new BatchFhirDeIdJobResult() 
                        : JsonConvert.DeserializeObject<BatchFhirDeIdJobResult>(jobInfo.Result);

                    var operations = _deIdOperationProvider.CreateDeIdOperations<StringBatchData, StringBatchData>(inputData.DeIdConfiguration);

                    var processors = operations.Select(operation => new FhirDeIdBatchProcessor(operation)).ToList();

                    job = new BatchFhirDeIdJob(inputData, result, _fhirDataLoader, processors, _fhirDataWriter,_loggerFactory.CreateLogger<BatchFhirDeIdJob>());
                    break;
                default:
                    throw new InvalidOperationException("Not support DataSourceType");
            }
            return job;
        }
    }
}
