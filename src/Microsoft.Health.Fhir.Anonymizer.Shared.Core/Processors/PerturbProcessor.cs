using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class PerturbProcessor : IAnonymizerProcessor
    {
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

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();
            var descendantsAndSelf = node.DescendantsAndSelf();

            foreach (var element in descendantsAndSelf)
            {
                // Perturb will not happen if value node is empty or visited.
                if (element.Value == null || context.VisitedNodes.Contains(element))
                {
                    continue;
                }

                if (!s_primitiveValueTypeNames.Contains(element.InstanceType, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new AnonymizerProcessingException(
                        $"Perturb is not applicable on node with type {element.InstanceType}. Only FHIR integer, decimal, unsignedInt and positiveInt are applicable.");
                }

                var perturbSetting = PerturbSetting.CreateFromRuleSettings(settings);
                AddNoise((ElementNode) element, perturbSetting);
                result.AddProcessRecord(AnonymizationOperations.Perturb, element);
            }

            return result;
        }

        private static void AddNoise(ElementNode node, PerturbSetting perturbSetting) 
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

            var noise = (decimal)ContinuousUniform.Sample(-1 * span / 2, span / 2);
            var perturbedValue = decimal.Round(originValue + noise, perturbSetting.RoundTo);
            if (perturbedValue <= 0 && string.Equals(FHIRAllTypes.PositiveInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
            {
                perturbedValue = 1;
            }
            if (perturbedValue < 0 && string.Equals(FHIRAllTypes.UnsignedInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
            {
                perturbedValue = 0;
            }
            node.Value = perturbedValue;
        }
    }
}