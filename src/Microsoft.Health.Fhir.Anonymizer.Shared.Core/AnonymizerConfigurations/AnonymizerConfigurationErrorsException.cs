using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerConfigurationErrorsException : Exception
    {
        public AnonymizerConfigurationErrorsException(string message) : base(message)
        {
        }

        public AnonymizerConfigurationErrorsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
