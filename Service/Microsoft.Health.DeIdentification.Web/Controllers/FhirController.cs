// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController
    {
        private IDeIdConfigurationStore _deIdConfigurationStore;
        private FhirDeIdHandler _handler;
        private ILogger<FhirController> _logger;

        public FhirController(
            IDeIdConfigurationStore deIdConfigurationStore,
            FhirDeIdHandler handler,
            ILogger<FhirController> logger)
        {
            _deIdConfigurationStore = EnsureArg.IsNotNull(deIdConfigurationStore, nameof(deIdConfigurationStore));
            _handler = EnsureArg.IsNotNull(handler, nameof(handler));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));

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
        public void BatchDeIdentification()
        {
            // Create Job
            // Return id to customer
        }

        // DELETE: Cancel batch job
        public void CancelDeIdentificationJob(string jobid)
        {
            // Cancel Job
            // Return status
        }

        // GET: Get job progress
        [HttpGet]
        [Route("/base/fhir")]
        public async Task<string> GetDeIdentificationJobStatus(string jobid)
        {
            return $"jobid is: " + jobid;
            // Get Job
            // Return job with progress
        }
    }
}
