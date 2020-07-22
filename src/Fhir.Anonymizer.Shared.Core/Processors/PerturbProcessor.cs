using System;
using EnsureThat;
using Fhir.Anonymizer.Core;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors.Settings;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distribution;

namespace Fhir.Anonymizer.Core.Processors
{
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private const HashSet<string> QuantityTypeNames = new HashSet<string> 
        {
            FHIRAllTypes.SimpleQuantity.ToString(),
            FHIRAllTypes.Age.ToString(),
            FHIRAllTypes.Duration.ToString()
        };
        private const HashSet<string> PrimitiveValueTypeNames = new HashSet<string> 
        {
            FHIRAllTypes.Integer.ToString(),
            HIRAllTypes.Decimal.ToString(),
            FHIRAllTypes.UnsignedInt.ToString(),
            HIRAllTypes.PositiveInt.ToString()
        };

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArgs.NotNull(node);
            EnsureArgs.NotNull(context?.VisitedNodes);
            EnsureArgs.NotNull(settings);

            var result = new ProcessResult();

            ElementNode valueNode;
            if (PrimitiveValueTypeNames.Contains(node.InstanceType))
            {
                valueNode = node;
            }
            else if (QuantityTypeNames.Contains(node.InstanceType))
            {
                valueNode = node.Children(Constants.ValueNodeName).FirstOrDefault();
            }
            else
            {
                return result;
            }

            var settings = PerturbSetting.CreateFromRuleSettings(settings);
            AddNoise(valueNode, settings);

            context.VisitedNodes.UnionWith(node.Descendants());
            result.AddProcessRecord(AnonymizationOperations.Perturb, node);
            return result;
        }

        public void AddNoise(ElementNode node, PerturbSetting settings) 
        {
            var originValue = decimal.Parse(valueNode.ToString());

            var span = settings.Span;
            if (settings.RangeType == PerturbRangeType.Proportional) 
            {
                span = originValue * settings.Span;
            }

            var noise = ContinousUniform.Sample(-1 * span, span);
            valueNode.Value = decimal.Round(originValue + noise, settings.RoundTo);
            return;
        }
    }
}