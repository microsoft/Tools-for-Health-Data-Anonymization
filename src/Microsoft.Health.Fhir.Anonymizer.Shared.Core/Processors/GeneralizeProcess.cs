using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using Hl7.Fhir.Model.Primitives;

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

        private void ValidateExpression(ElementNode node, string conditionExpression, string targetExpression)
        {          
            try
            {
                node.Predicate(conditionExpression);
                node.Scalar(targetExpression);
            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid cases expression {conditionExpression}", ex);
            }
        }

        private void GeneralizeIntegerNode(ElementNode node, string targetExpression)
        {
            if (node.Scalar(targetExpression).GetType() != typeof(Int64))
            {
                throw new AnonymizerConfigurationErrorsException("Invalid datatype. Expect an integer.");
            }

            int targetIntValue = Convert.ToInt32(node.Scalar(targetExpression));

            if (string.Equals(FHIRAllTypes.PositiveInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase) && targetIntValue <= 0)
            {
                node.Value = 1;
            }
            else if (string.Equals(FHIRAllTypes.UnsignedInt.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase) && targetIntValue < 0)
            {
                node.Value = 0;
            }
            else
            {
                node.Value = targetIntValue;
            }
        }

        private void GeneralizeDateOrTimerNode(ElementNode node, string targetExpression)
        {
            var targetValue = node.Scalar(targetExpression);
            if (string.Equals(targetValue.GetType().ToString(), node.InstanceType))
            {
                node.Value = targetValue;
                return;
            }
            try
            {
                if (string.Equals(FHIRAllTypes.Time.ToString(), node.InstanceType, StringComparison.InvariantCultureIgnoreCase))
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
                throw new AnonymizerConfigurationErrorsException($"Invalid data types or format of  expression {targetExpression} output", ex);
            }

        }

        private ProcessResult GeneralizeNode(ElementNode node, string targetExpression, ProcessResult result)
        {
            if (node.Scalar(targetExpression) == null)
            {
                node.Value = null;
                result.AddProcessRecord(AnonymizationOperations.Generalize, node);
                return result;
            }

            if (s_integerValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                GeneralizeIntegerNode(node, targetExpression);
            }
            else if(s_DateOrTimeValueTypeNames.Contains(node.InstanceType, StringComparer.InvariantCultureIgnoreCase))
            {
                GeneralizeDateOrTimerNode(node, targetExpression);
            }
            else
            {
                node.Value = node.Scalar(targetExpression);
            }

            result.AddProcessRecord(AnonymizationOperations.Generalize, node);
            return result;
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

            JObject Cases = null;
            try
            {
                Cases = JObject.Parse(generalizeSetting.Cases);

            }
            catch (Exception ex)
            {
                throw new AnonymizerConfigurationErrorsException($"Invalid cases {generalizeSetting.Cases}", ex);
            }     

            foreach (var eachCase in Cases)
            {

                string conditionExpression = eachCase.Key;
                string targetExpression = eachCase.Value.ToString();

                ValidateExpression(node, conditionExpression, targetExpression);

                if (node.Predicate(conditionExpression))
                {
                    result = GeneralizeNode(node, targetExpression, result);
                    return result;
                }
            }

            string otherValues = generalizeSetting.OtherValues;

            if (string.Equals(otherValues, "redact", StringComparison.InvariantCultureIgnoreCase))
            {
                node.Value = null;
            }
            result.AddProcessRecord(AnonymizationOperations.Generalize, node);
            return result;
        }



    }
}