using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors.Settings;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;

namespace Fhir.Anonymizer.Core.Processors
{
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private static readonly HashSet<string> QuantityTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Age.ToString(),
            FHIRAllTypes.Duration.ToString(),
            FHIRAllTypes.Distance.ToString(),
            FHIRAllTypes.Money.ToString(),
            FHIRAllTypes.Quantity.ToString(),
            FHIRAllTypes.SimpleQuantity.ToString()
        };

        private static readonly HashSet<string> PrimitiveValueTypeNames = new HashSet<string> 
        {
            FHIRAllTypes.Decimal.ToString(),
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();

            ElementNode valueNode;
            if (PrimitiveValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node;
            }
            else if (QuantityTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                valueNode = node.Children(Constants.ValueNodeName).Cast<ElementNode>().FirstOrDefault();
            }
            else
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
            var originValue = decimal.Parse(node.Value.ToString());

            var span = perturbSetting.Span;
            if (perturbSetting.RangeType == PerturbRangeType.Proportional) 
            {
                span = (double)originValue * perturbSetting.Span;
            }

            var noise = (decimal)ContinuousUniform.Sample(-1 * span, span);
            node.Value = decimal.Round(originValue + noise, perturbSetting.RoundTo);
            return;
        }
    }
}