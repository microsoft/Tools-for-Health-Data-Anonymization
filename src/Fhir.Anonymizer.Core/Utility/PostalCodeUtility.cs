using System.Collections.Generic;
using System.Linq;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Utility
{
    public class PostalCodeUtility
    {
        private static readonly char s_replacementDigit = '0';
        private static readonly int s_initialDigitsCount = 3;

        public static ProcessResult RedactPostalCode(ElementNode node, bool enablePartialZipCodesForRedact = false, List<string> restrictedZipCodeTabulationAreas = null)
        {
            var processResult = new ProcessResult();
            if (!node.IsPostalCodeNode())
            {
                return processResult;
            }

            var originalValue = node.Value?.ToString();
            if (enablePartialZipCodesForRedact)
            {
                if (restrictedZipCodeTabulationAreas != null && restrictedZipCodeTabulationAreas.Any(x => node.Value.ToString().StartsWith(x)))
                {
                    node.Value = new string(s_replacementDigit, node.Value.ToString().Length);
                }
                else if (node.Value.ToString().Length >= s_initialDigitsCount)
                {
                    node.Value = $"{node.Value.ToString().Substring(0, s_initialDigitsCount)}{new string(s_replacementDigit, node.Value.ToString().Length - s_initialDigitsCount)}";
                }
                processResult.Summary.UpdateIsAbstracted(originalValue, node.Value?.ToString());
            }
            else
            {
                node.Value = null;
                processResult.Summary.UpdateIsRedacted(originalValue, node.Value?.ToString());
            }

            return processResult;
        }
    }
}
