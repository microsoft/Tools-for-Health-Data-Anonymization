using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    internal class MockAnonymizerProcessor : IAnonymizerProcessor
    {
        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            throw new System.NotImplementedException();
        }
    }
}