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
using Microsoft.Health.Fhir.Anonymizer.Core;
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
        public FhirController()
        {
            _operationProvider = new FhirDeIdOperationProvider();
        }

        // Post: 
        [HttpPost]
        [Route("/base/fhir")]
        public string DeIdentification(string deidConfiguration, [FromBody] Object resource)
        {
            var jsonPath = "configuration-sample.json";
            var operation = _operationProvider.CreateDeIdOperationFromJson<string, string>(jsonPath);
            var result = operation.Process(resource.ToString());
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

        private DeIdConfiguration GenerateDeIdRuleSet(string deidConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}
