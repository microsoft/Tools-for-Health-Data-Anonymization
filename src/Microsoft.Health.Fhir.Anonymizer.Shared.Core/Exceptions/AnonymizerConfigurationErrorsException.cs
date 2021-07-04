using System;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Exceptions
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
