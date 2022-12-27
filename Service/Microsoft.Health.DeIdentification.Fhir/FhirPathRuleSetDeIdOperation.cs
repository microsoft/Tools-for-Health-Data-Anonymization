// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.DeIdentification.Contract;
using Microsoft.Health.Fhir.Anonymizer.Core;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.DeIdentification.Fhir
{
    public class FhirPathRuleSetDeIdOperation : IDeIdOperation<ResourceList, ResourceList>
    {
        private readonly AnonymizerEngine _anonymizerEngine;

        public FhirPathRuleSetDeIdOperation(AnonymizerEngine anonymizerEngine)
        {
            _anonymizerEngine = EnsureArg.IsNotNull(anonymizerEngine, nameof(anonymizerEngine));
        }

        public ResourceList Process(ResourceList source)
        {
            var result = new ResourceList();
            result.Resources = new List<JObject>();
            foreach (var item in source.Resources)
            {
                result.Resources.Add(JObject.Parse(ProcessSingle(item.ToString())));
            }

            return result;

        }

        public string ProcessSingle(string context)
        {
            return _anonymizerEngine.AnonymizeJson(context);
        }
    }
}