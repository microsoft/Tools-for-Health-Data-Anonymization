using System;
using System.Collections.Generic;
using System.Text;
using Fhir.Anonymizer.Core.Visitors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTests.Visitors
{
    public class ElementNodeAcceptTests
    {
        class TestVisitor : AbstractAnonymizationVisitor
        {
            public List<string> Result { get; set; } = new List<string>();

            public int PreVisitEntryNodeCount { get; set; } = 0;

            public int PostVisitEntryNodeCount { get; set; } = 0;

            public int PreVisitContainedNodeCount { get; set; } = 0;

            public int PostVisitContainedNodeCount { get; set; } = 0;

            public override bool Visit(ElementNode node)
            {
                Result.Add(node.Name);

                return base.Visit(node);
            }

            public override void PreVisitEntryNode(ElementNode node)
            {
                PreVisitEntryNodeCount++;
            }

            public override void PostVisitEntryNode(ElementNode node)
            {
                PostVisitEntryNodeCount++;
            }

            public override void PreVisitContainedNode(ElementNode node)
            {
                PreVisitContainedNodeCount++;
            }

            public override void PostVisitContainedNode(ElementNode node)
            {
                PostVisitContainedNodeCount++;
            }
        };

        [Fact]
        public void GivenAnBundleNode_WhenAcceptVisitor_AllNodesShouldBeVisitInOrder()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var node = ElementNode.FromElement(parser.Parse(_bundleTestContent).ToTypedElement());

            var testVisitor = new TestVisitor();
            node.Accept(testVisitor);

            Assert.Equal("Bundle,id,type,resource,id,text,status,div,identifier,system,value,resource,text,status,div", string.Join(',', testVisitor.Result));
            Assert.Equal(2, testVisitor.PreVisitEntryNodeCount);
            Assert.Equal(2, testVisitor.PostVisitEntryNodeCount);
            Assert.Equal(0, testVisitor.PreVisitContainedNodeCount);
            Assert.Equal(0, testVisitor.PostVisitContainedNodeCount);
        }

        [Fact]
        public void GivenAnContainedNode_WhenAcceptVisitor_AllNodesShouldBeVisitInOrder()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var node = ElementNode.FromElement(parser.Parse(_containedTestContent).ToTypedElement());

            var testVisitor = new TestVisitor();
            node.Accept(testVisitor);

            Assert.Equal("Condition,contained,id,name,family,given,contained,id,name,family,asserter,reference", string.Join(',', testVisitor.Result));
            Assert.Equal(2, testVisitor.PreVisitContainedNodeCount);
            Assert.Equal(2, testVisitor.PostVisitContainedNodeCount);
            Assert.Equal(0, testVisitor.PreVisitEntryNodeCount);
            Assert.Equal(0, testVisitor.PostVisitEntryNodeCount);
        }

        private const string _containedTestContent =
@"
{
  ""resourceType"": ""Condition"",
  ""contained"": [
    {
      ""resourceType"": ""Practitioner"",
      ""id"": ""p1"",
      ""name"": [
        {
          ""family"": ""Person2"",
          ""given"": [ ""Patricia1"" ]
        }
      ]
    },
    {
      ""resourceType"": ""Practitioner"",
      ""id"": ""p2"",
      ""name"": [
        {
          ""family"": ""Person""
        }
      ]
    }
  ],
  ""asserter"": {
    ""reference"": ""#p1""
  }
}";

private const string _bundleTestContent =
@"
{
  ""resourceType"": ""Bundle"",
  ""id"": ""bundle-references"",
  ""type"": ""collection"",
  ""entry"": [
    {
      ""fullUrl"": ""http://example.org/fhir/Patient/23"",
      ""resource"": {
        ""resourceType"": ""Patient"",
        ""id"": ""23"",
        ""text"": {
          ""status"": ""generated"",
          ""div"": ""<div xmlns=\""http://www.w3.org/1999/xhtml\""><p><b>Generated Narrative with Details</b></p><p><b>id</b>: 23</p><p><b>identifier</b>: 1234567</p></div>""
        },
        ""identifier"": [
          {
            ""system"": ""http://example.org/ids"",
            ""value"": ""1234567""
          }
        ]
      }
    },
    {
      ""fullUrl"": ""urn:uuid:04121321-4af5-424c-a0e1-ed3aab1c349d"",
      ""resource"": {
        ""resourceType"": ""Patient"",
        ""text"": {
          ""status"": ""generated"",
          ""div"": ""<div xmlns=\""http://www.w3.org/1999/xhtml\""><p><b>Generated Narrative with Details</b></p></div>""
        }
      }
    }
  ]
}
";
    }
}
