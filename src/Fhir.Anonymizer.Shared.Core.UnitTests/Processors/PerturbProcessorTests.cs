using System;
using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
using Fhir.Anonymizer.Core.Processors.Settings;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Newtonsoft.Json;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class PerturbProcessorTests
    {
        public static IEnumerable<object[]> GetPrimitiveNodesToPerturbFixedSpan()
        {
            yield return new object[] { new Integer(5), 0, 0, 0, 0};
            yield return new object[] { new FhirDecimal(5.0), 0, 2, 5.00, 5.00 };
            yield return new object[] { new FhirDecimal(5.234), 3, 2, 2.23, 8.23 };
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveNodesToPerturbFixedValue))]
        public void GivenAPrimitiveNode_WhenPerturbFixedSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<ElementNode>()
            };
            var settings = new Dictionary<string, object> { {RuleKeys.Span, span}, {RuleKeys.RoundTo, roundTo} };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Value.ToString());
            Assert.True(perturbedValue >= lowerBound);
            Assert.True(perturbedValue <= upperBound);
        }
    }
}
