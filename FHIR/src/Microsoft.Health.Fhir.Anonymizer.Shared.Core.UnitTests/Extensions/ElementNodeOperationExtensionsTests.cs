using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Extensions
{
    public class ElementNodeOperationExtensionsTests
    {
        private readonly PocoStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();

        [Fact]
        public void GivenAnElementNode_WhenRemoveNullChildren_NullChildrenShouldBeRemoved()
        {
            var node = GetSampleNode();
            Assert.Equal(2, node.Children().Count());

            node.RemoveNullChildren();
            Assert.Equal(2, node.Children().Count());

            node.Children("child1").CastElementNodes().First().Value = null;
            node.RemoveNullChildren();
            Assert.Single(node.Children());

            node.Children("child2").CastElementNodes().First().Value = null;
            node.RemoveNullChildren();
            Assert.Empty(node.Children());
        }

        [Fact]
        public void GivenAResourceWithoutSecurityLabels_WhenTryAddSecurityLabels_SecurityLabelsShouldBeAdded()
        {
            var resource = new Patient();
            var result = new ProcessResult();
            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            Assert.Single(resourceNode.Children("meta"));

            resource = resourceNode.ToPoco<Patient>();

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
            var result = new ProcessResult();
            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            Assert.Single(resourceNode.Children("meta"));

            resource = resourceNode.ToPoco<Patient>();

            Assert.Equal(2, resource.Meta.Security.Count);
            Assert.Equal("MASKED", resource.Meta.Security[0].Code);
            Assert.Equal(SecurityLabels.REDACT.Code, resource.Meta.Security[1].Code);
        }

        [Fact]
        public void GivenAResourceWithVersionId_WhenTryAddSecurityLabels_VersionIdShouldBeKept()
        {
            var resource = new Patient()
            {
                Meta = new Meta()
                {
                    VersionId = "Test"
                }
            };
            var result = new ProcessResult();

            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();

            Assert.Equal("Test", resource.Meta.VersionId);
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
            var result = new ProcessResult();
            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();

            Assert.Single(resource.Meta.Security);
            Assert.Equal(SecurityLabels.REDACT.Code, resource.Meta.Security.First().Code);
        }

        [Fact]
        public void GivenAResourceWithNoSecurityLabels_WhenTryAddMultipleSecurityLabels_SecurityLabelsShouldBeAddedAgain()
        {
            var resource = new Patient();
            var result = new ProcessResult();

            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));
            result.AddProcessRecord(AnonymizationOperations.Abstract, ElementNode.ForPrimitive(1));
            result.AddProcessRecord(AnonymizationOperations.Perturb, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();

            Assert.Equal(3, resource.Meta.Security.Count);
            Assert.Contains(SecurityLabels.REDACT.Code, resource.Meta.Security.Select(s => s.Code));
            Assert.Contains(SecurityLabels.ABSTRED.Code, resource.Meta.Security.Select(s => s.Code));
            Assert.Contains(SecurityLabels.PERTURBED.Code, resource.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAResourceWithSecurityLabels_WhenTryAddMultipleSecurityLabels_SecurityLabelsShouldBeAddedAgain()
        {
            var resource = new Patient()
            {
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding() { Code = "REDACTED" },
                        new Coding() { Code = "ADDITION" }
                    }
                }
            };
            var result = new ProcessResult();

            result.AddProcessRecord(AnonymizationOperations.Redact, ElementNode.ForPrimitive(1));
            result.AddProcessRecord(AnonymizationOperations.Abstract, ElementNode.ForPrimitive(1));
            result.AddProcessRecord(AnonymizationOperations.Perturb, ElementNode.ForPrimitive(1));

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();

            Assert.Equal(4, resource.Meta.Security.Count);
            Assert.Contains(SecurityLabels.REDACT.Code, resource.Meta.Security.Select(s => s.Code));
            Assert.Contains(SecurityLabels.ABSTRED.Code, resource.Meta.Security.Select(s => s.Code));
            Assert.Contains(SecurityLabels.PERTURBED.Code, resource.Meta.Security.Select(s => s.Code));
        }

        [Fact]
        public void GivenAResource_WhenTryToAddSecuritytagWithNoResult_MetaShouldNotBeChange()
        {
            var resource = new Patient()
            {
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding() { Code = "REDACTED" },
                        new Coding() { Code = "ADDITION" }
                    }
                }
            };
            var result = new ProcessResult();

            var resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();

            Assert.Equal(2, resource.Meta.Security.Count);
            Assert.Contains(SecurityLabels.REDACT.Code, resource.Meta.Security.Select(s => s.Code));
            Assert.Contains("ADDITION", resource.Meta.Security.Select(s => s.Code));

            resource = new Patient();
            result = new ProcessResult();
            resourceNode = ElementNode.FromElement(resource.ToTypedElement());
            resourceNode.AddSecurityTag(result);
            resource = resourceNode.ToPoco<Patient>();
            Assert.Null(resource.Meta);
        }

        private ElementNode GetSampleNode()
        {
            var root = ElementNode.FromElement(new FhirString("root").ToTypedElement());
            var child1 = ElementNode.FromElement(new FhirString("child1").ToTypedElement());
            var child2 = ElementNode.FromElement(new FhirString("child2").ToTypedElement());
            root.Name = "root";
            child1.Name = "child1";
            child2.Name = "child2";
            root.Add(_provider, child1);
            root.Add(_provider, child2);

            return root;
        }
    }
}
