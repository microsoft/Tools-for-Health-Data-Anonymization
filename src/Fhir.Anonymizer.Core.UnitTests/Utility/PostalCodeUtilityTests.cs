using System.Collections.Generic;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests
{
    public class PostalCodeUtilityTests
    {
        public static IEnumerable<object[]> GetPostalCodeDataForRedact()
        {
            yield return new object[] { "98052", "00000" };
            yield return new object[] { "10104", "00000" };
            yield return new object[] { "00000", "00000" };
        }

        public static IEnumerable<object[]> GetPostalCodeDataForPartialRedact()
        {
            yield return new object[] { "98052", "98000" };
            yield return new object[] { "10104", "10100" };
            yield return new object[] { "20301", "00000" };
            yield return new object[] { "55602", "00000" };
        }

        [Theory]
        [MemberData(nameof(GetPostalCodeDataForRedact))]
        public void GivenAPostalCode_WhenRedact_ThenDigitsShouldBeRedacted(string postalCode, string expectedPostalCode)
        {
            var node = ElementNode.FromElement(new FhirString(postalCode).ToTypedElement());
            node.Name = "postalCode";
            PostalCodeUtility.RedactPostalCode(node, false, null);

            Assert.Equal(expectedPostalCode.ToString(), node.Value);
        }

        [Theory]
        [MemberData(nameof(GetPostalCodeDataForPartialRedact))]
        public void GivenAPostalCode_WhenPartialRedact_ThenPartialDigitsShouldBeRedacted(string postalCode, string expectedPostalCode)
        {
            var node = ElementNode.FromElement(new FhirString(postalCode).ToTypedElement());
            node.Name = "postalCode";
            PostalCodeUtility.RedactPostalCode(node, true, new List<string>() { "203", "556" });

            Assert.Equal(expectedPostalCode.ToString(), node.Value);
        }
    }
}
