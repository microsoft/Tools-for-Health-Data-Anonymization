using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class ResourceExtensionsTests
    {
        [Fact]
        public void GivenAResourceWithoutSecurityLabels_WhenTryAddSecurityLabels_SecurityLabelsShouldBeAdded()
        {
            var resource = new Patient();
            var result = new ProcessResult()
            {
                IsRedacted = true
            };

            resource.TryAddSecurityLabels(result);

            Assert.Single(resource.Meta.Security);
            Assert.Equal(SecurityLabels.REDACT.Code, resource.Meta.Security.First().Code);
        }

        [Fact]
        public void GivenAResourceWithDifferentSecurityLabels_WhenTryAddSecurityLabels_SecurityLabelsShouldBeAddedWithoutRemovingOriginalOnes()
        {
            var resource = new Patient()
            {
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding() { Code = "MASKED" }
                    }
                }
            };
            var result = new ProcessResult()
            {
                IsRedacted = true
            };

            resource.TryAddSecurityLabels(result);
            Assert.Equal(2, resource.Meta.Security.Count);
            Assert.Equal("MASKED", resource.Meta.Security[0].Code);
            Assert.Equal(SecurityLabels.REDACT.Code, resource.Meta.Security[1].Code);
        }

        [Fact]
        public void GivenAResourceWithSameSecurityLabels_WhenTryAddSecurityLabels_SecurityLabelsShouldNotBeAddedAgain()
        {
            var resource = new Patient()
            {
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding() { Code = "REDACTED" }
                    }
                }
            };
            var result = new ProcessResult()
            {
                IsRedacted = true
            };

            resource.TryAddSecurityLabels(result);
            Assert.Single(resource.Meta.Security);
            Assert.Equal(SecurityLabels.REDACT.Code, resource.Meta.Security.First().Code);
        }
    }
}
