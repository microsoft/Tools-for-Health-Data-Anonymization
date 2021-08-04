using System.Collections.Generic;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class RedactProcessor : IAnonymizerProcessor
    {
        public bool EnablePartialDatesForRedact { get; set; } = false;

        public bool EnablePartialAgesForRedact { get; set; } = false;

        public bool EnablePartialZipCodesForRedact { get; set; } = false;

        public List<string> RestrictedZipCodeTabulationAreas { get; set; } = null;

        public RedactProcessor(bool enablePartialDatesForRedact, bool enablePartialAgesForRedact, bool enablePartialZipCodesForRedact, List<string> restrictedZipCodeTabulationAreas)
        {
            EnablePartialDatesForRedact = enablePartialDatesForRedact;
            EnablePartialAgesForRedact = enablePartialAgesForRedact;
            EnablePartialZipCodesForRedact = enablePartialZipCodesForRedact;
            RestrictedZipCodeTabulationAreas = restrictedZipCodeTabulationAreas;
        }

        public static RedactProcessor Create(AnonymizerConfigurationManager configuratonManager)
        {
            var parameters = configuratonManager.GetParameterConfiguration();
            return new RedactProcessor(parameters.EnablePartialDatesForRedact, parameters.EnablePartialAgesForRedact,
                parameters.EnablePartialZipCodesForRedact, parameters.RestrictedZipCodeTabulationAreas);
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            var result = new ProcessResult();
            var descendantsAndSelf = node.DescendantsAndSelf();

            foreach (var element in descendantsAndSelf)
            {
                if (element.Value == null || context?.VisitedNodes != null && context.VisitedNodes.Contains(element))
                {
                    continue;
                }

                var elementNode = (ElementNode) element;

                if (elementNode.IsDateNode())
                {
                    result.Update(DateTimeUtility.RedactDateNode(elementNode, EnablePartialDatesForRedact));
                }
                else if (elementNode.IsDateTimeNode() || elementNode.IsInstantNode())
                {
                    result.Update(DateTimeUtility.RedactDateTimeAndInstantNode(elementNode, EnablePartialDatesForRedact));
                }
                else if (elementNode.IsAgeDecimalNode())
                {
                    result.Update(DateTimeUtility.RedactAgeDecimalNode(elementNode, EnablePartialAgesForRedact));
                }
                else if (elementNode.IsPostalCodeNode())
                {
                    result.Update(PostalCodeUtility.RedactPostalCode(elementNode, EnablePartialZipCodesForRedact, RestrictedZipCodeTabulationAreas));
                }
                else
                {
                    elementNode.Value = null;
                    result.AddProcessRecord(AnonymizationOperations.Redact, elementNode);
                }
            }

            return result;
        }
    }
}
