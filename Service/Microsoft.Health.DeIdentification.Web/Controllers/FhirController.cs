// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Local;
using Newtonsoft.Json;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController
    {
        private FhirDeIdOperationProvider _operationProvider;
        private IArtifactStore _artifactStore;
        private IDeIdConfigurationStore _deidConfigurationStore;
        private FhirDeIdHandler _handler;
        public FhirController()
        {
            _operationProvider = new FhirDeIdOperationProvider();
            _artifactStore = new LocalArtifactStore();
            _deidConfigurationStore = new DeIdConfigurationStore(_artifactStore);
            _handler = new FhirDeIdHandler();
        }

        // Post: 
        [HttpPost]
        [Route("/fhirR4")]
        public async Task<string> DeIdentification(string deidConfiguration, [FromBody] ResourceList resources)
        {
            var configuration = _deidConfigurationStore.GetByName(deidConfiguration);
            var operations = _operationProvider.CreateDeIdOperations(configuration);
            var result = await _handler.ExecuteProcess(operations, resources.Resources);
            return JsonConvert.SerializeObject(result);
        }

        // Post: start batch job
        public void BatchDeIdentification()
        {
            // Create BatchFhirDeIdJobInputData with 
            // Call queue client to enqueue Job
            // Return id to customer
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
            return $"jobid is: " + jobid;
            // Get Job
            // Return job with progress
        }
    }
}
