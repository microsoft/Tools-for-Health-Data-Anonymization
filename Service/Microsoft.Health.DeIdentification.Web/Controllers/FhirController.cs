// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using Microsoft.Health.DeIdentification.Local;
using Microsoft.Health.DeIdentification.Web.Models;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController
    {
        private FhirDeIdOperationProvider _operationProvider;
        private IArtifactStore _artifactStore;
        private IDeIdConfigurationStore _deidConfigurationStore;
        public FhirController()
        {
            _operationProvider = new FhirDeIdOperationProvider();
            _artifactStore = new LocalArtifactStore();
            _deidConfigurationStore = new DeIdConfigurationStore(_artifactStore);
        }

        // Post: 
        [HttpPost]
        [Route("/fhirR4")]
        public async Task<string> DeIdentification(string deidConfiguration, [FromBody] ResourceList resources)
        {
            var configuration = _deidConfigurationStore.GetByName(deidConfiguration);
            var operations = _operationProvider.CreateDeIdOperations(configuration);
            var result = await _operationProvider.ExecuteProcess((List<FhirDeIdOperation>)operations, resources.Resources);
            return result;
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
