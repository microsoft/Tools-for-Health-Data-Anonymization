using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors.Settings;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;

namespace Fhir.Anonymizer.Core.Processors
{
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private static readonly HashSet<string> s_quantityTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Age.ToString(),
            FHIRAllTypes.Duration.ToString(),
            FHIRAllTypes.Distance.ToString(),
            FHIRAllTypes.Money.ToString(),
            FHIRAllTypes.Quantity.ToString(),
            FHIRAllTypes.SimpleQuantity.ToString()
        };

        private static readonly HashSet<string> s_primitiveValueTypeNames = new HashSet<string> 
        {
            FHIRAllTypes.Decimal.ToString(),
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        private static readonly HashSet<string> s_integerValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        private static readonly HashSet<string> s_positiveValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();

            ElementNode valueNode = null;
            if (s_primitiveValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node;
            }
            else if (s_quantityTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node.Children(Constants.ValueNodeName).Cast<ElementNode>().FirstOrDefault();
            }
            
            // Perturb will not happen if value node is empty or visited.
            if (valueNode?.Value == null || context.VisitedNodes.Contains(valueNode))
            {
                return result;
            }

            var perturbSetting = PerturbSetting.CreateFromRuleSettings(settings);

            AddNoise(valueNode, perturbSetting);
            context.VisitedNodes.UnionWith(node.Descendants().Cast<ElementNode>());
            result.AddProcessRecord(AnonymizationOperations.Perturb, node);
            return result;
        }

        public void AddNoise(ElementNode node, PerturbSetting perturbSetting) 
        {
            if (s_integerValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                perturbSetting.RoundTo = 0;
            }

            var originValue = decimal.Parse(node.Value.ToString());
            var span = perturbSetting.Span;
            if (perturbSetting.RangeType == PerturbRangeType.Proportional) 
            {
                span = (double)originValue * perturbSetting.Span;
            }

            var noise = (decimal)ContinuousUniform.Sample(-1 * span, span);
            var perturbedValue = decimal.Round(originValue + noise, perturbSetting.RoundTo);
            if (perturbedValue < 0 && IsPositiveValueNode(node))
            {
                perturbedValue = 0;
            }

            node.Value = perturbedValue;
            return;
        }

        private bool IsPositiveValueNode(ElementNode node)
        {
            return s_positiveValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}