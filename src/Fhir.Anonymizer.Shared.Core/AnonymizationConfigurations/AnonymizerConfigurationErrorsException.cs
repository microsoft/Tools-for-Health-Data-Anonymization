using System;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
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
