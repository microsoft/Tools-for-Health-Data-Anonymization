using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
{
    public class AnonymizerRuleNotApplicableException : AnonymizerConfigurationErrorsException
    {
        public AnonymizerRuleNotApplicableException(string message) : base(message)
        {
        }

        public AnonymizerRuleNotApplicableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
