// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Batch.Model;
using Microsoft.Health.DeIdentification.Batch.Models.Data;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.JobManagement;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController: ControllerBase
    {
        private IDeIdConfigurationRegistration _deIdConfigurationStore;
        private FhirDeIdHandler _handler;
        private ILogger<FhirController> _logger;
        private FhirDeIdBatchHandler _batchHandler;

        public FhirController(
            IDeIdConfigurationRegistration deIdConfigurationStore,
            FhirDeIdHandler handler,
            ILogger<FhirController> logger, 
            FhirDeIdBatchHandler batchHandler)
        {
            _deIdConfigurationStore = EnsureArg.IsNotNull(deIdConfigurationStore, nameof(deIdConfigurationStore));
            _handler = EnsureArg.IsNotNull(handler, nameof(handler));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _batchHandler = batchHandler;
        }

        // Post: 
        [HttpPost]
        [Route("/base/deidentify/fhirR4")]
        [Produces("application/json")]
        public async Task<IActionResult> DeIdentification(string deidConfiguration, [FromBody] JsonBatchData resources)
        {
            var configuration = _deIdConfigurationStore.GetByName(deidConfiguration);
            
            // TODO: validate configurations
            if (configuration == null)
            {
                _logger.LogInformation("The configuration is null.");
                return BadRequest();
            }
            else
            {
                var result = await _handler.ProcessRequestAsync(configuration, resources);
                return Ok(result);
            }
        }

        // Post: start batch job
        [HttpPost]
        [Route("/base/deidentify/dataset/fhirR4")]
        public async Task<IActionResult> BatchDeIdentification(string deidConfiguration, [FromBody] BatchDeIdRequestBody requestBody)
        {
            // Create BatchFhirDeIdJobInputData with 
            // Call queue client to enqueue Job
            // Return id to customer
            var configuration = _deIdConfigurationStore.GetByName(deidConfiguration);
            Request.Headers.TryGetValue("Host", out var host);
            RouteNames.BaseUrl = host;
            RouteNames.Protocol = Request.IsHttps ? "https://" : "http://";

            if (configuration == null)
            {
                _logger.LogInformation("The configuration is null.");
                return BadRequest();
            }
            else
            {
                var operationId = await _batchHandler.ProcessRequestAsync(configuration, requestBody);
                string url = GenerateUrl(operationId);
                return Accepted(url);

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
        [Route("/base/operation/{operationId}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDeIdentificationJobStatus(string operationId)
        {
            // queue client getjobbyid
            // Get Job
            // Return job with progress
            var jobInfo = await _batchHandler.GetJobStatusById(operationId);
            if (jobInfo == null)
            {
                return NotFound();
            }
            if (jobInfo.Status == JobStatus.Completed)
            {
                return Ok(JObject.Parse(jobInfo.Result));
            }
            else
            {
                string url = GenerateUrl(operationId);

                if (string.IsNullOrWhiteSpace(jobInfo.Result))
                {
                    return Accepted(url);
                }
                else
                {
                    return Accepted(url, JObject.Parse(jobInfo.Result));
                }
            }
        }

        private static string GenerateUrl(string operationId) => $"{RouteNames.Protocol}{RouteNames.BaseUrl}/base/operation/{operationId}";
    }
}
