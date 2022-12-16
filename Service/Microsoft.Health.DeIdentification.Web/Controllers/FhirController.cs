// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.DeIdentification.Fhir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Controllers
{
    public class FhirController
    {
        // Post: 
        [HttpPost]
        [Route("/base/fhir")]
        public async Task<string> DeIdentification(string deidRuleSet, [FromBody] string resource)
        {
            var operation = new FhirDeIdOperation("configuration-sample.json");
            return operation.Process(resource);
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

        private DeIdRuleSet GenerateDeIdRuleSet(string deidRuleSet)
        {
            throw new NotImplementedException();
        }
    }
}
