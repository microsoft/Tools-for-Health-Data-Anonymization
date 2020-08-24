using System;
using System.Collections.Generic;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public partial class GeneralizeProcessor : IAnonymizerProcessor
    {      
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
            var nativeType=Primitives.GetNativeRepresentation(node.InstanceType);           
            node.Value = PrimitiveTypeConverter.ConvertTo(node.Value, nativeType);
            foreach (var eachCase in generalizeSetting.Cases)
            {
                try
                {
                    if (node.Predicate(eachCase.Key))
                    {
                        node.Value = node.Scalar(eachCase.Value.ToString());
                        result.AddProcessRecord(AnonymizationOperations.Generalize, node);
                        return result;
                    }                   
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException || ex is FormatException)
                    {
                        throw new AnonymizerConfigurationErrorsException($"Invalid cases expression {eachCase}.", ex);
                    }
                    throw;
                }
            }

            if (generalizeSetting.OtherValues==GeneralizationOtherValuesOperation.redact)
            {
                node.Value = null;
            }

            result.AddProcessRecord(AnonymizationOperations.Generalize, node);
            return result;
        }
    }
}