using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Utility
{
    public class ReferenceUtilityTests
    {
        private static readonly Func<string, string> _transformation = (id) => "@ID";
        public static IEnumerable<object[]> GetReferenceData()
        {
            yield return new object[] { "#p1", "#@ID" };
            yield return new object[] { "Patient/034AB16", "Patient/@ID" };
            yield return new object[] { "http://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b", 
                "http://fhir.hl7.org/svc/StructureDefinition/@ID" };
            yield return new object[] { "http://example.org/fhir/Observation/apo89654/_history/2", 
                "http://example.org/fhir/Observation/@ID/_history/2" };
            yield return new object[] { "urn:uuid:C757873D-EC9A-4326-A141-556F43239520",
                "urn:uuid:@ID" };
            yield return new object[] { "urn:oid:1.2.3.4.5",
                "urn:oid:@ID" };
        }

        // Transform whole reference if reference values are not conformed 
        public static IEnumerable<object[]> GetUnknownReferenceData()
        {
            yield return new object[] { "034AB16", "@ID" };
            yield return new object[] { "Patient/AbcŴΉЙ", "@ID" };
            yield return new object[] { "http://fhir.hl7.org/svc/StructureDefinitionTest/c8973a22-2b5b-4e76-9c66-00639c99e61b",
                "@ID" };
            yield return new object[] { "ftp://fhir.hl7.org/svc/StructureDefinition/c8973a22-2b5b-4e76-9c66-00639c99e61b",
                "@ID" };
            yield return new object[] { "wwurn:uuid:c757873d-ec9a-4326-a141-556f43239520",
                "@ID" };
            yield return new object[] { "urn:oid:1.2.3=4.5", "@ID" };
        }

        [Theory]
        [MemberData(nameof(GetReferenceData))]
        public void GivenAKnownReference_WhenTransform_CorrectPartShouldBeTransformed(string reference, string expected)
        {
            var newReference = ReferenceUtility.TransformReferenceId(reference, _transformation);
            Assert.Equal(expected, newReference);
        }

        [Theory]
        [MemberData(nameof(GetUnknownReferenceData))]
        public void GivenAnUnknownReference_WhenTransform_WholeReferenceShouldBeTransformed(string reference, string expected)
        {
            var newReference = ReferenceUtility.TransformReferenceId(reference, _transformation);
            Assert.Equal(expected, newReference);
        }
    }
}
