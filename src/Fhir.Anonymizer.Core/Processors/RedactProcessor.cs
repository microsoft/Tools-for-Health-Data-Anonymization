using System.Collections.Generic;
using Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
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

        public void Process(ElementNode node)
        {
            if (node.IsDateNode())
            {
                DateTimeUtility.RedactDateNode(node, EnablePartialDatesForRedact);
            }
            else if (node.IsDateTimeNode() || node.IsInstantNode())
            {
                DateTimeUtility.RedactDateTimeAndInstantNode(node, EnablePartialDatesForRedact);
            }
            else if (node.IsAgeDecimalNode())
            {
                DateTimeUtility.RedactAgeDecimalNode(node, EnablePartialAgesForRedact);
            }
            else if (node.IsPostalCodeNode())
            {
                PostalCodeUtility.RedactPostalCode(node, EnablePartialZipCodesForRedact, RestrictedZipCodeTabulationAreas);
            }
            else
            {
                node.Value = null;
            }
        }
    }
}
