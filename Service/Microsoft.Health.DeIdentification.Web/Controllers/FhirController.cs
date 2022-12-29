// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Batch.Models;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Fhir.Models;
using Microsoft.Health.DeIdentification.Local;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json;
using System.Net;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController
    {
        private IDeIdConfigurationStore _deIdConfigurationStore;
        private FhirDeIdHandler _handler;
        private ILogger<FhirController> _logger;
        private IQueueClient _client;
        private LocalFhirBatchHandler _batchHandler;

        public FhirController(
            IDeIdConfigurationStore deIdConfigurationStore,
            FhirDeIdHandler handler,
            ILogger<FhirController> logger, 
            IQueueClient client,
            LocalFhirBatchHandler batchHandler)
        {
            _deIdConfigurationStore = EnsureArg.IsNotNull(deIdConfigurationStore, nameof(deIdConfigurationStore));
            _handler = EnsureArg.IsNotNull(handler, nameof(handler));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _client = client;
            _batchHandler = batchHandler;
        }

        // Post: 
        [HttpPost]
        [Route("/fhirR4")]
        public async Task<string> DeIdentification(string deidConfiguration, [FromBody] ResourceList resources)
        {
            var configuration = _deIdConfigurationStore.GetByName(deidConfiguration);
            
            // TODO: validate configurations
            if (configuration == null)
            {
                _logger.LogInformation("The configuration is null.");
                return string.Empty;
            }
            else
            {
                var result = await _handler.ProcessRequestAsync(configuration, resources);
                return JsonConvert.SerializeObject(result);
            }
        }

        // Post: start batch job
        [HttpPost]
        [Route("/base/deidentify/dataset/fhir")]
        public async Task<IActionResult> BatchDeIdentification(string deidConfiguration, [FromBody] BatchInputData requestBody)
        {
            // Create BatchFhirDeIdJobInputData with 
            // Call queue client to enqueue Job
            // Return id to customer
            var configuration = _deIdConfigurationStore.GetByName(deidConfiguration);

            if (configuration == null)
            {
                _logger.LogInformation("The configuration is null.");
                return BatchResult.BadRequest();
            }
            else
            {
                var result = await _batchHandler.ProcessRequestAsync(configuration, requestBody);
                IDictionary<string, string> headers = new Dictionary<string, string>();
                headers["Content-Type"] = "application/json";
                headers["Accept"] = "application/json";
                headers["Url"] = string.Empty;
                foreach (var item in result)
                {
                    headers["Url"] = headers["Url"] + "https://localhost:7007/base/fhir?jobid=" + item.Id + ",";
                }
                return BatchResult.Accept(headers);
            }
        }

        // DELETE: Cancel batch job
        public void CancelDeIdentificationJob(string jobid)
        {
            // queue client cancel job by id
            // Cancel Job
            // Return status
        }

        // GET: Get job progress
        [HttpGet]
        [Route("/base/fhir")]
        public async Task<string> GetDeIdentificationJobStatus(string jobid)
        {
            // queue client getjobbyid
            // Get Job
            // Return job with progress
            var jobInfo = await _client.GetJobByIdAsync(0, long.Parse(jobid), true, new CancellationToken());
            if(jobInfo == null)
            {
                return string.Empty;
            }
            else
            {
                return jobInfo.Result; 
            }
        }
    }
}
