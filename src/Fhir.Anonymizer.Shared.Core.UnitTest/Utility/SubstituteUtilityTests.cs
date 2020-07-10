using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Fhir.Anonymizer.Core.UnitTest.Utility
{
    public class SubstituteUtilityTests
    {
        [Fact]
        public static void GivenAnAdressNode_WhenSubstitute_CorrectNodeShouldBeReturned()
        {
            var node = GetAddressNode();

            var address = new Address
            {
                Use = Address.AddressUse.Home,
                State = "Maryland",
                Period = new Period
                {
                    Start = "2020-01-01T00:00:00Z",
                }
            };
            var replaceNode = ElementNode.FromElement(address.ToTypedElement());

            SubstituteUtility.SubstituteNode(node, replaceNode, new HashSet<ElementNode>(), new HashSet<ElementNode>());
            Assert.Equal(node.ToString(), replaceNode.ToString());
        }

        [Fact]
        public static void GivenAnAddressNode_WhenSubstituteWithEmptyNode_CorrectNodeShouldBeReturned()
        {
            var node = GetAddressNode();
            var replaceNode = ElementNode.FromElement(new Address().ToTypedElement());
            SubstituteUtility.SubstituteNode(node, replaceNode, new HashSet<ElementNode>(), new HashSet<ElementNode>());
            Assert.Equal(node.ToString(), replaceNode.ToString());
        }

        [Fact]
        public static void GivenAnAddressNode_WhenSubstituteWithVisitedNodes_CorrectNodeShouldBeReturned()
        {
            var node = GetAddressNode();
            var replaceNode = ElementNode.FromElement(new Address().ToTypedElement());
            var visitedNodes = new HashSet<ElementNode>();
            visitedNodes.UnionWith(node.Select("Address.period.start").Cast<ElementNode>());
            visitedNodes.UnionWith(node.Select("Address.city").Cast<ElementNode>());
            var keepNodes = new HashSet<ElementNode>();
            _ = SubstituteUtility.ShouldKeepNodeDuringSubstitution(node, visitedNodes, keepNodes);

            SubstituteUtility.SubstituteNode(node, replaceNode, visitedNodes, keepNodes);

            var targetAddress = new Address
            {
                City = "Seattle",
                Period = new Period
                {
                    Start = "2020-01-01T00:00:00Z",
                }
            };
            var targetNode = ElementNode.FromElement(targetAddress.ToTypedElement());
            Assert.Equal(node.ToString(), targetNode.ToString());
        }

        [Fact]
        public static void GivenANodeAndVisitedNodes_WhenSubstituting_CorrectNodesShouldBeKept()
        {
            var node = GetAddressNode();
            var visitedNodes = new HashSet<ElementNode>();
            visitedNodes.UnionWith(node.Select("Address.period.start").Cast<ElementNode>());
            visitedNodes.UnionWith(node.Select("Address.city").Cast<ElementNode>());

            var keepNodes = new HashSet<ElementNode>();
            var result = SubstituteUtility.ShouldKeepNodeDuringSubstitution(node, visitedNodes, keepNodes);

            Assert.True(result);
            Assert.Equal(4, keepNodes.Count);
            Assert.Contains(node, keepNodes);
            Assert.Contains(node.Select("Address.period").Cast<ElementNode>().First(), keepNodes);
            Assert.DoesNotContain(node.Select("Address.period.end").Cast<ElementNode>().First(), keepNodes);
            Assert.DoesNotContain(node.Select("Address.state").Cast<ElementNode>().First(), keepNodes);
        }

        [Fact]
        public static void GivenANode_WhenMarkVisited_AllNodesShouldBeConvered()
        {
            var node = GetAddressNode();
            var visitedNodes = new HashSet<ElementNode>();
            SubstituteUtility.MarkSubstitutedFragementAsVisited(node, visitedNodes);
            
            Assert.Equal(8, visitedNodes.Count);
        }

        private static ElementNode GetAddressNode()
        {
            var address = new Address
            {
                Use = Address.AddressUse.Work,
                State = "Washington",
                City = "Seattle",
                PostalCode = "98052",
                Period = new Period
                {
                    Start = "2020-01-01T00:00:00Z",
                    End = "2020-12-30T11:59:59Z"
                }
            };

            return ElementNode.FromElement(address.ToTypedElement());
        }
    }
}
