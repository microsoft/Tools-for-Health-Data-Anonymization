using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Processors
{
    public class PerturbProcessorTests
    {
        public static IEnumerable<object[]> GetPrimitiveNodesToPerturbFixedSpan()
        {
            yield return new object[] { new Integer(5), 0, 0, 5, 5};
            yield return new object[] { new FhirDecimal((decimal)5.0), 0, 2, 5, 5 };
            yield return new object[] { new FhirDecimal((decimal)5.234), 6, 2, 2.23, 8.23 };
            yield return new object[] { new FhirDecimal((decimal)12e-2), 2, 2, -0.88, 1.12 };
        }

        public static IEnumerable<object[]> GetPrimitiveNodesToPerturbProportionalSpan()
        {
            yield return new object[] { new Integer(5), 0, 0, 5, 5 };
            yield return new object[] { new Integer(5), 0.4, 0, 4, 6 };
            yield return new object[] { new FhirDecimal(5), 2, 2, 0, 10 };
            yield return new object[] { new FhirDecimal((decimal)5.234), 1, 2, 2.62, 7.85 };
            yield return new object[] { new FhirDecimal((decimal)12e-2), 4, 2, -0.12, 0.36 };
        }

        public static IEnumerable<object[]> GetIntegerNodesToPerturbFixedSpan()
        {
            yield return new object[] { new Integer(100), 0, 0, 100, 100 };
            yield return new object[] { new UnsignedInt(5000), 100, 2, 4950, 5050 };
            yield return new object[] { new PositiveInt(1000000), 2000, 2, 999000, 1001000 };

            // Positive nodes should greater than zero. Unsigned nodes should not less than zero.
            yield return new object[] { new UnsignedInt(10), 40, 0, 0, 30 };
            yield return new object[] { new PositiveInt(1), 200, 2, 1, 101 };
        }

        public static IEnumerable<object[]> GetIntegerNodesToPerturbProportionalSpan()
        {
            yield return new object[] { new Integer(100), 0, 0, 100, 100 };
            yield return new object[] { new UnsignedInt(5000), 0.02, 2, 4950, 5050 };
            yield return new object[] { new PositiveInt(1000000), 0.002, 2, 999000, 1001000 };

            // Positive nodes should greater than zero. Unsigned nodes should not less than zero.
            yield return new object[] { new UnsignedInt(10), 4, 0, 0, 30 };
            yield return new object[] { new PositiveInt(1), 200, 2, 1, 101 };
        }

        public static IEnumerable<object[]> GetQuantityNodesToPerturbFixedSpan()
        {
            yield return new object[]
            {
                new Age { Value = 20 },
                0,
                2,
                20,
                20
            };
            yield return new object[] 
            {
                new Distance { Value = (decimal)1024.12345678 },
                0.00002,
                4,
                1024.1234,
                1024.1235
            };
            yield return new object[] 
            {
                new Duration { Value = (decimal)0.0001 },
                0.00002,
                2,
                0,
                0
            };
            yield return new object[] 
            {
                new Money { Value = (decimal)7000.12345678 },
                2000,
                4,
                6000.1235,
                8000.1235
            };
            yield return new object[] 
            {
                new Quantity { Value = decimal.Parse("25,162.1378") },
                2.26,
                2,
                25161.01,
                25163.27
            };
        }

        public static IEnumerable<object[]> GetQuantityNodesToPerturbProportionalSpan()
        {
            yield return new object[] 
            {
                new Age { Value = 20 },
                0,
                2,
                20,
                20
            };
            yield return new object[] 
            {
                new Distance { Value = (decimal)1024.12345678 },
                0.2,
                4,
                921.7111,
                1126.5358
            };
            yield return new object[] 
            {
                new Duration { Value = (decimal)0.0001 },
                2000,
                2,
                -0.1,
                0.1
            };
            yield return new object[] 
            {
                new Money { Value = (decimal)100 },
                2000,
                4,
                -99900,
                100100
            };
            yield return new object[] 
            {
                new Quantity { Value = decimal.Parse("25,162.1378") },
                1,
                2,
                12581.07,
                37743.21
            };
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveNodesToPerturbFixedSpan))]
        public void GivenAPrimitiveNode_WhenPerturbFixedSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { {"span", span}, {"roundTo", roundTo} };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) <= roundTo);
        }

        [Theory]
        [MemberData(nameof(GetPrimitiveNodesToPerturbProportionalSpan))]
        public void GivenAPrimitiveNode_WhenPerturbProportionalSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { { "span", span }, { "roundTo", roundTo }, { "rangeType", "proportional" } };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) <= roundTo);
        }

        [Theory]
        [MemberData(nameof(GetQuantityNodesToPerturbFixedSpan))]
        public void GivenAQuantityNode_WhenPerturbFixedSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { { "span", span }, { "roundTo", roundTo } };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Children("value").First().Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) <= roundTo);
        }

        [Theory]
        [MemberData(nameof(GetQuantityNodesToPerturbProportionalSpan))]
        public void GivenAQuantityNode_WhenPerturbProportionalSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { { "span", span }, { "roundTo", roundTo }, { "rangeType", "proportional" } };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Children("value").First().Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) <= roundTo);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodesToPerturbFixedSpan))]
        public void GivenAnIntegerNode_WhenPerturbFixedSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { { "span", span }, { "roundTo", roundTo } };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) == 0);
        }

        [Theory]
        [MemberData(nameof(GetIntegerNodesToPerturbProportionalSpan))]
        public void GivenAnIntegerNode_WhenPerturbProportionalSpan_PerturbedNodeShouldBeReturned(Base data, decimal span, decimal roundTo, decimal lowerBound, decimal upperBound)
        {
            var node = ElementNode.FromElement(data.ToTypedElement());
            PerturbProcessor processor = new PerturbProcessor();
            var context = new ProcessContext
            {
                VisitedNodes = new HashSet<string>()
            };
            var settings = new Dictionary<string, object> { { "span", span }, { "roundTo", roundTo }, { "rangeType", "proportional" } };

            var processResult = processor.Process(node, context, settings);
            Assert.True(processResult.IsPerturbed);
            var perturbedValue = decimal.Parse(node.Value.ToString());
            Assert.InRange(perturbedValue, lowerBound, upperBound);
            Assert.True(GetDecimalPlaces(perturbedValue) == 0);
        }

        private int GetDecimalPlaces(decimal n)
        {
            n = Math.Abs(n);
            n -= (int)n;
            var decimalPlaces = 0;
            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }
            return decimalPlaces;
        }
    }
}
