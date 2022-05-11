using System;
using System.Collections.Generic;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors.Settings;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public partial class GeneralizeProcessor : IAnonymizerProcessor
    {
        private readonly DateShiftProcessor dateShiftProcessor;
        private readonly CryptoHashProcessor cryptoHashProcessor;
        private readonly EncryptProcessor encryptProcessor;
        private readonly SubstituteProcessor substituteProcessor;
        private readonly PerturbProcessor perturbProcessor;

        public GeneralizeProcessor()
        {

        }

        public GeneralizeProcessor(DateShiftProcessor _dateShiftProcessor, CryptoHashProcessor _cryptoHashProcessor, EncryptProcessor _encryptProcessor, SubstituteProcessor _substituteProcessor, PerturbProcessor _perturbProcessor)        {
            dateShiftProcessor = _dateShiftProcessor;
            cryptoHashProcessor = _cryptoHashProcessor;
            encryptProcessor = _encryptProcessor;
            substituteProcessor = _substituteProcessor;
            perturbProcessor = _perturbProcessor;
        }
        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node);
            EnsureArg.IsNotNull(context?.VisitedNodes);
            EnsureArg.IsNotNull(settings);

            var result = new ProcessResult();
            if (!ModelInfo.IsPrimitive(node.InstanceType))
            {
                throw new AnonymizerRuleNotApplicableException(
                    $"Generalization is not applicable on the node with type {node.InstanceType}. Only FHIR primitive nodes (ref: https://www.hl7.org/fhir/datatypes.html#primitive) are applicable.");
            }

            if (node.Value == null)
            {
                return result;
            }

            var generalizeSetting = GeneralizeSetting.CreateFromRuleSettings(settings);
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
                    throw new AnonymizerProcessingException($"Generalize failed when processing {eachCase}.", ex);
                }
            }

            if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.Redact)
            {
                node.Value = null;
            }
            else if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.CryptoHash)
            {
                cryptoHashProcessor.Process(node);
            }
            else if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.DateShift)
            {
                dateShiftProcessor.Process(node);
            }
            else if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.Encrypt)
            {
                encryptProcessor.Process(node);
            }
            else if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.Substitute)
            {
                substituteProcessor.Process(node);
            }
            else if (generalizeSetting.OtherValues == GeneralizationOtherValuesOperation.Perturb)
            {
                perturbProcessor.Process(node);
            }

            result.AddProcessRecord(AnonymizationOperations.Generalize, node);
            return result;
        }
    }
}