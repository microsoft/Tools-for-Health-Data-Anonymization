using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Hl7.Fhir.Model.Primitives;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public partial class GeneralizeProcessor : IAnonymizerProcessor
    {
        private static readonly HashSet<string> s_integerValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.Integer.ToString(),
            FHIRAllTypes.PositiveInt.ToString(),
            FHIRAllTypes.UnsignedInt.ToString()
        };

        private static readonly HashSet<string> s_DateOrTimeValueTypeNames = new HashSet<string>
        {
            FHIRAllTypes.DateTime.ToString(),
            FHIRAllTypes.Date.ToString(),
            FHIRAllTypes.Time.ToString(),
            FHIRAllTypes.Instant.ToString()
        };

        private void GeneralizeIntegerNode(ElementNode node, object targetValue)
        {
            //Transform the target value into int32.
            int target;
            try
            {
                target = Convert.ToInt32(targetValue);
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid data types or format of expression {targetValue} output", ex);
            }

            //Check target value for positive integer and unsigned interger.
            if (string.Equals(FHIRAllTypes.PositiveInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase) && target <= 0)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid target value {target} for positive int data type.");
            }
            else if (string.Equals(FHIRAllTypes.UnsignedInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase) && target < 0)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid target value {target} for unsigned int data type.");
            }
            else
            {
                node.Value = target;
            }
        }

        private void GeneralizeDateOrTimeNode(ElementNode node, object targetValue)
        {
            if (targetValue.GetType() == node.Value.GetType())
            {
                node.Value = targetValue;
            }
            else
            {
                //Transform the target value into partialTime or partialDateTime types
                try
                {
                    if (IsTimeNode(node))
                    {
                        node.Value = PartialTime.Parse(targetValue.ToString());
                    }
                    else
                    {
                        node.Value = PartialDateTime.Parse(targetValue.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid data types or format of expression {targetValue} output", ex);
                }
            } 
        }

        private void GeneralizeNode(ElementNode node, object targetValue)
        {
            if (targetValue == null)
            {
                node.Value = null;
            }
            
            //Generalization for different data types.
            if (s_integerValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                GeneralizeIntegerNode(node, targetValue);
            }
            else if(s_DateOrTimeValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                GeneralizeDateOrTimeNode(node, targetValue);
            }
            else
            {
                node.Value = targetValue;
            }
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();
            if (!ModelInfo.IsPrimitive(node.InstanceType) || node.Value == null)
            {
                return result;
            }

            var generalizeSetting = GeneralizeSetting.CreateFromRuleSettings(settings);
            foreach (var eachCase in generalizeSetting.Cases)
            {
                bool matchCondition;
                object targetValue;
                try
                {
                    matchCondition = node.Predicate(eachCase.Key);
                    targetValue = node.Scalar(eachCase.Value.ToString());
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid cases expression {eachCase}.", ex);
                }

                if (matchCondition)
                {
                    GeneralizeNode(node, targetValue);
                    result.AddProcessRecord(AnonymizationOperations.Generalize, node);
                    return result;
                }
            }

            if (generalizeSetting.OtherValues == GeneralizationOtherValuesType.redact)
            {
                node.Value = null;
            }

            result.AddProcessRecord(AnonymizationOperations.Generalize, node);
            return result;
        }

        private bool IsTimeNode(ElementNode node)
        {
            return string.Equals(FHIRAllTypes.Time.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}